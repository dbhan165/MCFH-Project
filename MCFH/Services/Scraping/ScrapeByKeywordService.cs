using System.Collections.Concurrent;
using MCFH.Configuration;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Models.Scraping;
using MCFH.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System.Text.Json;

namespace MCFH.Services.Scraping;

public class ScrapeByKeywordService
{
    private readonly McfhDbContext _db;
    private readonly AiAnalysisService _aiAnalysisService;
    private readonly ScrapeOptions _scrapeOptions;
    private readonly ProxyRotationService _proxyRotation;
    private readonly ProxyOptions _proxyOptions;
    private readonly SerpApiNewsDiscovery _serpApiNewsDiscovery;
    private readonly ICommentBundleStorage _bundleStorage;
    private ScrapeOptions _activeOptions = null!;
    private SystemProxy? _activeProxy;
    private string? _scrapeJobId;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private ConcurrentDictionary<string, byte>? _scrapedKeys;
    private int _skippedCount;

    public ScrapeByKeywordService(
        McfhDbContext db,
        AiAnalysisService aiAnalysisService,
        IOptions<ScrapeOptions> scrapeOptions,
        ProxyRotationService proxyRotation,
        IOptions<ProxyOptions> proxyOptions,
        SerpApiNewsDiscovery serpApiNewsDiscovery,
        ICommentBundleStorage bundleStorage)
    {
        _db = db;
        _aiAnalysisService = aiAnalysisService;
        _scrapeOptions = scrapeOptions.Value;
        _proxyRotation = proxyRotation;
        _proxyOptions = proxyOptions.Value;
        _serpApiNewsDiscovery = serpApiNewsDiscovery;
        _bundleStorage = bundleStorage;
    }

    public async Task<ScrapeByKeywordResult> ScrapeAsync(
        int projectId,
        ScrapeJobProgress? progress = null,
        ScrapeTimeFilter? timeFilter = null,
        bool fastDemo = false,
        string? scrapeJobId = null)
    {
        var result = new ScrapeByKeywordResult();
        timeFilter ??= new ScrapeTimeFilter();
        _activeOptions = ResolveOptions(fastDemo);
        var allowUnknownDates = _activeOptions.FastDemoMode;
        _scrapeJobId = scrapeJobId;
        _activeProxy = null;

        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
        {
            result.ErrorMessage = "Project không tồn tại.";
            return result;
        }

        if (project.IsDeleted == true)
        {
            result.ErrorMessage = "Project đã bị xóa.";
            return result;
        }

        // Nếu workspace đã xóa mềm thì không được cào tiếp.
        var workspace = await _db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkspaceId == project.WorkspaceId && w.IsDeleted != true);
        if (workspace == null)
        {
            result.ErrorMessage = "Workspace đã bị xóa.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(project.SearchQuery))
        {
            result.ErrorMessage = "Project chưa setup keyword (search_query rỗng).";
            return result;
        }

        try
        {
            await EnsureProxyAsync(rotate: false);
            if (_activeProxy != null)
                progress?.SetPhase("scraping", $"Proxy {_activeProxy.IpAddress} — chuẩn bị cào...");

            var keyword = project.SearchQuery;
            result.Keyword = keyword;

            var enableNews = await ProjectEnablesNewsAsync(projectId);

            progress?.InitPlatforms(
                project.EnableFacebook == true,
                project.EnableYoutube == true,
                project.EnableTiktok == true,
                enableNews);
            progress?.SetPhase("scraping", _activeOptions.FastDemoMode
                ? $"Chế độ demo nhanh — cào «{keyword}»..."
                : timeFilter.IsActive
                    ? $"Đang cào từ khóa «{keyword}» (trong {timeFilter.PostedSinceDays} ngày gần đây)..."
                    : $"Đang cào từ khóa «{keyword}»...");

            _scrapedKeys = await LoadExistingScrapeKeysAsync(projectId);
            _skippedCount = 0;

            var hasAnySource = project.EnableFacebook == true
                               || project.EnableYoutube == true
                               || project.EnableTiktok == true
                               || enableNews;
            if (!hasAnySource)
            {
                result.ErrorMessage = "Project chưa bật nguồn dữ liệu nào.";
                return result;
            }

            var parallelTasks = new List<Task>();

            if (project.EnableFacebook == true)
                parallelTasks.Add(RunFacebookAsync(projectId, keyword, result, progress, timeFilter, allowUnknownDates));

            if (project.EnableYoutube == true)
                parallelTasks.Add(RunYouTubeAsync(projectId, keyword, result, progress, timeFilter, allowUnknownDates));

            if (enableNews)
                parallelTasks.Add(RunNewsAsync(projectId, keyword, result, progress, timeFilter, allowUnknownDates));

            if (project.EnableTiktok == true && _activeOptions.FastDemoMode && _activeOptions.FastDemoRunTikTokParallel)
                parallelTasks.Add(RunTikTokAsync(projectId, keyword, result, progress, timeFilter, allowUnknownDates));

            if (parallelTasks.Count > 0)
            {
                if (_scrapeOptions.ParallelPlatforms && parallelTasks.Count > 1)
                    await Task.WhenAll(parallelTasks);
                else
                    foreach (var task in parallelTasks)
                        await task;
            }

            if (progress?.IsCancellationRequested == true)
                return await FinalizeResultAsync(projectId, result, progress, cancelled: true);

            if (project.EnableTiktok == true && !(_activeOptions.FastDemoMode && _activeOptions.FastDemoRunTikTokParallel))
            {
                await EnsureProxyAsync(rotate: _proxyOptions.RotatePerPlatform);
                await RunTikTokAsync(projectId, keyword, result, progress, timeFilter, allowUnknownDates);
            }

            return await FinalizeResultAsync(projectId, result, progress, progress?.IsCancellationRequested == true);
        }
        finally
        {
            if (_activeProxy != null)
            {
                var total = result.Facebook.Count + result.YouTube.Count + result.TikTok.Count + result.News.Count;
                if (total > 0)
                    await _proxyRotation.RecordSuccessAsync(_activeProxy.ProxyId);
                else if (result.Errors.Count > 0 || !string.IsNullOrWhiteSpace(result.ErrorMessage))
                    await _proxyRotation.RecordFailureAsync(_activeProxy.ProxyId);
            }

            _activeProxy = null;
            _scrapeJobId = null;
            _scrapedKeys = null;
            _skippedCount = 0;
            _activeOptions = _scrapeOptions;
        }
    }

    private async Task EnsureProxyAsync(bool rotate)
    {
        if (!_proxyRotation.IsEnabled)
            return;

        if (rotate && _activeProxy != null)
        {
            _activeProxy = await _proxyRotation.RotateAsync(_activeProxy, _scrapeJobId, markCurrentAsFailed: false);
            return;
        }

        if (_activeProxy != null)
            return;

        _activeProxy = await _proxyRotation.AcquireNextAsync();
        if (_activeProxy != null && !string.IsNullOrWhiteSpace(_scrapeJobId))
            await _proxyRotation.AssignToJobAsync(_scrapeJobId, _activeProxy.ProxyId);
    }

    private Proxy? CurrentPlaywrightProxy() =>
        ProxyRotationService.ToPlaywrightProxy(_activeProxy);

    private ScrapeOptions ResolveOptions(bool fastDemoRequest)
    {
        if (!fastDemoRequest && !_scrapeOptions.FastDemoMode)
            return _scrapeOptions;

        var json = JsonSerializer.Serialize(_scrapeOptions);
        var opts = JsonSerializer.Deserialize<ScrapeOptions>(json)!;
        opts.FastDemoMode = true;
        return opts;
    }

    private async Task<ConcurrentDictionary<string, byte>> LoadExistingScrapeKeysAsync(int projectId)
    {
        var rows = await _db.ScrapedFeedbacks
            .AsNoTracking()
            .Where(f => f.ProjectId == projectId
                        && f.IsDeleted != true
                        && f.OriginalUrl != null
                        && f.OriginalUrl != "")
            .Select(f => new { f.Platform, f.OriginalUrl })
            .ToListAsync();

        var keys = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = ScrapeUrlHelper.DedupeKey(row.Platform ?? "", row.OriginalUrl!);
            if (!string.IsNullOrWhiteSpace(key))
                keys.TryAdd(key, 0);
        }

        Console.WriteLine($"[Scrape] Đã có {keys.Count} URL trong project #{projectId} — sẽ bỏ qua khi cào lại.");
        return keys;
    }

    private void NoteSkipped()
    {
        Interlocked.Increment(ref _skippedCount);
    }

    private bool IsAlreadyScraped(string platform, string url)
    {
        if (string.IsNullOrWhiteSpace(url) || _scrapedKeys == null)
            return false;

        var key = ScrapeUrlHelper.DedupeKey(platform, url);
        return !string.IsNullOrWhiteSpace(key) && _scrapedKeys.ContainsKey(key);
    }

    private void MarkScraped(string platform, string url)
    {
        if (string.IsNullOrWhiteSpace(url) || _scrapedKeys == null)
            return;

        var key = ScrapeUrlHelper.DedupeKey(platform, url);
        if (!string.IsNullOrWhiteSpace(key))
            _scrapedKeys.TryAdd(key, 0);
    }

    private async Task<ScrapeByKeywordResult> FinalizeResultAsync(
        int projectId, ScrapeByKeywordResult result, ScrapeJobProgress? progress, bool cancelled)
    {
        var totalSaved = result.Facebook.Count + result.YouTube.Count + result.TikTok.Count + result.News.Count;

        if (cancelled)
        {
            progress?.SkipPendingPlatforms("Đã bỏ qua — người dùng dừng");
            result.Message = totalSaved > 0
                ? $"Đã dừng — giữ {totalSaved} bài (FB: {result.Facebook.Count}, YT: {result.YouTube.Count}, TT: {result.TikTok.Count}, News: {result.News.Count})."
                : "Đã dừng — chưa lưu bài nào.";
            result.Errors.Add("Đã dừng theo yêu cầu người dùng.");
            return result;
        }

        if (totalSaved == 0)
        {
            if (_skippedCount > 0 && result.Errors.Count == 0)
            {
                result.Message =
                    $"Không có bài mới — đã bỏ qua {_skippedCount} bài/video đã cào trước đó.";
            }
            else if (result.Errors.Count == 0)
            {
                result.Errors.Add(
                    "Không thu thập được dữ liệu. Kiểm tra Playwright, cookie Facebook và từ khóa.");
            }
        }
        else
        {
            var totalComments = result.Facebook.Sum(p => p.CommentsCount)
                + result.YouTube.Sum(p => p.CommentsCount)
                + result.TikTok.Sum(p => p.CommentsCount);

            result.Message =
                $"Đã lưu {totalSaved} bản ghi + {totalComments} bình luận text (FB: {result.Facebook.Count}, YT: {result.YouTube.Count}, TT: {result.TikTok.Count}, News: {result.News.Count}).";

            progress?.SetPhase("analyzing", "Đang phân tích AI các bài vừa cào...");
            if (_activeOptions.FastDemoMode && _activeOptions.FastDemoSkipAiAnalysis)
            {
                result.Analysis = new AnalyzeProjectResultDto
                {
                    ProjectId = projectId,
                    Message = "Chế độ demo: bỏ qua phân tích AI — chạy Analyze sau khi demo."
                };
            }
            else
            {
                result.Analysis = await _aiAnalysisService.AnalyzePendingFeedbacksAsync(projectId, force: false);
            }
            if (result.Analysis.AnalyzedCount > 0)
                result.Message += $" {result.Analysis.Message}";
            else if (!string.IsNullOrWhiteSpace(result.Analysis.Message))
                result.Message += $" {result.Analysis.Message}";
        }

        return result;
    }

    private async Task RunFacebookAsync(
        int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress,
        ScrapeTimeFilter timeFilter, bool allowUnknownDates)
    {
        var targetCount = _activeOptions.EffectiveMaxFacebookPosts;
        var poolSize = timeFilter.DiscoveryPoolSize(targetCount);
        var facebookUrls = await GetFacebookGroupUrlsAsync(projectId);
        if (facebookUrls.Count == 0)
        {
            progress?.FailPlatform("facebook", "Chưa có URL group/page Facebook. Nhập URL ở bước 2 khi tạo project (bật Facebook) hoặc thêm FB_SOURCES.");
            lock (result.Errors)
                result.Errors.Add("Facebook: Chưa có URL group/page. Nhập link group khi tạo project (chọn Facebook) hoặc cấu hình FB_SOURCES.");
            return;
        }

        Console.WriteLine($"[Facebook] URLs: {string.Join(" | ", facebookUrls)}");

        progress?.StartPlatform("facebook", "Đang tìm bài trên Facebook...");
        var skipped = 0;

        foreach (var groupUrl in facebookUrls)
        {
            if (progress?.IsCancellationRequested == true)
                break;

            try
            {
                var searchUrl = FacebookUrlHelper.BuildKeywordSearchUrl(groupUrl, keyword);
                var feedUrl = FacebookUrlHelper.BuildFeedUrl(groupUrl);

                var scraper = new FacebookGroupScraper();
                var posts = await scraper.ScrapeAsync(searchUrl, poolSize, _activeOptions, proxy: CurrentPlaywrightProxy());

                if (posts.Count == 0 && !string.Equals(searchUrl, feedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Facebook] Search rỗng — thử feed group: {feedUrl}");
                    posts = await scraper.ScrapeAsync(feedUrl, poolSize, _activeOptions, proxy: CurrentPlaywrightProxy());
                }

                foreach (var post in posts)
                {
                    if (progress?.IsCancellationRequested == true)
                        break;
                    if (result.Facebook.Count >= targetCount)
                        break;

                    var postUrl = post.PostUrl ?? "";
                    if (!string.IsNullOrWhiteSpace(postUrl) && IsAlreadyScraped("facebook", postUrl))
                    {
                        NoteSkipped();
                        skipped++;
                        Console.WriteLine($"[Facebook] Skip existing: {postUrl}");
                        continue;
                    }

                    if (!timeFilter.IsWithinRange(post.PostedAt, allowUnknownDates))
                    {
                        Console.WriteLine($"[Facebook] Skip ngoài khoảng thời gian: {postUrl} posted={post.PostedAt}");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(keyword)
                        && !post.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        && posts.Count > targetCount)
                    {
                        continue;
                    }

                    var fbMax = Math.Max(_activeOptions.EffectiveFacebookMaxComments, _activeOptions.EffectiveMaxCommentsPerItem);
                    var commentTexts = CommentTextHelper.FilterFacebook(post.Comments, fbMax);

                    if (!_activeOptions.FastDemoMode
                        && commentTexts.Count < 3
                        && !string.IsNullOrWhiteSpace(post.PostUrl))
                    {
                        var fbCommentScraper = new FacebookCommentScraper();
                        var extra = await fbCommentScraper.ScrapePostCommentsAsync(
                            post.PostUrl!, _activeOptions, CurrentPlaywrightProxy());
                        commentTexts = CommentTextHelper.FilterFacebook(commentTexts.Concat(extra), fbMax);
                    }

                    var (feedbackId, savedComments, wasSkipped) = await SaveFeedbackAsync(
                        projectId, "facebook", post.Text, post.Author, postUrl, commentTexts, post.PostedAt);

                    if (wasSkipped)
                    {
                        skipped++;
                        continue;
                    }

                    lock (result.Facebook)
                    {
                        result.Facebook.Add(new PlatformPostResult
                        {
                            FeedbackId = feedbackId,
                            Author = post.Author,
                            Text = post.Text,
                            Url = post.PostUrl ?? "",
                            CommentsCount = savedComments
                        });
                    }

                    progress?.UpdatePlatform(
                        "facebook",
                        result.Facebook.Count,
                        skipped > 0
                            ? $"Facebook: đã lưu {result.Facebook.Count} bài (bỏ qua {skipped} bài cũ)..."
                            : $"Facebook: đã lưu {result.Facebook.Count} bài...");
                }
            }
            catch (Exception ex)
            {
                var hint = PlaywrightErrorHint(ex);
                progress?.FailPlatform("facebook", $"{ex.Message}{hint}");
                lock (result.Errors)
                    result.Errors.Add($"Facebook ({groupUrl}): {ex.Message}{hint}");
            }
        }

        if (result.Facebook.Count > 0)
            progress?.CompletePlatform("facebook", result.Facebook.Count,
                skipped > 0 ? $"Hoàn tất — {result.Facebook.Count} bài mới (+{skipped} đã cào trước)" : null);
        else if (skipped > 0)
            progress?.CompletePlatform("facebook", 0, $"Facebook: {skipped} bài đã cào trước đó — bỏ qua");
        else if (!result.Errors.Any(e => e.StartsWith("Facebook", StringComparison.OrdinalIgnoreCase)))
            progress?.CompletePlatform("facebook", 0, "Facebook: không tìm thấy bài phù hợp");
    }

    private async Task RunYouTubeAsync(
        int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress,
        ScrapeTimeFilter timeFilter, bool allowUnknownDates)
    {
        var targetCount = _activeOptions.EffectiveMaxVideosPerPlatform;
        var poolSize = timeFilter.DiscoveryPoolSize(targetCount);
        progress?.StartPlatform("youtube", "Đang tìm video trên YouTube...");
        try
        {
            var searchScraper = new YouTubeSearchScraper();
            var useProxyForYouTube = _activeProxy != null;
            List<string> urls;
            try
            {
                urls = await RunYouTubeSearchAsync(searchScraper, keyword, poolSize, timeFilter, useProxyForYouTube);
            }
            catch (Exception ex) when (IsNavigationTimeout(ex) && useProxyForYouTube)
            {
                Console.WriteLine("[YouTube] Timeout qua proxy — thử lại không dùng proxy...");
                progress?.UpdatePlatform("youtube", 0, "YouTube: proxy chậm, thử lại trực tiếp...");
                useProxyForYouTube = false;
                urls = await RunYouTubeSearchAsync(searchScraper, keyword, poolSize, timeFilter, useProxy: false);
            }

            using var playwright = await Playwright.CreateAsync();
            var ytProxy = useProxyForYouTube ? CurrentPlaywrightProxy() : null;
            await using var browser = await playwright.Chromium.LaunchAsync(
                PlaywrightScrapeHelper.YouTubeLaunch(_activeOptions, ytProxy));

            var commentScraper = new YouTubeScraper(NullLoggerFactory.Instance.CreateLogger<YouTubeScraper>());
            var ytMaxComments = Math.Max(_activeOptions.EffectiveYouTubeMaxComments, _activeOptions.EffectiveMaxCommentsPerItem);

            if (urls.Count == 0)
            {
                progress?.CompletePlatform("youtube", 0, "YouTube: không tìm thấy video");
                lock (result.Errors)
                    result.Errors.Add("YouTube: Không tìm thấy video cho từ khóa này.");
                return;
            }

            progress?.UpdatePlatform("youtube", 0, $"YouTube: tìm thấy {urls.Count} video, đang lấy comment...");

            Console.WriteLine($"[YouTube] Found {urls.Count} video(s), target {targetCount}");

            var skipped = 0;
            foreach (var url in urls)
            {
                if (progress?.IsCancellationRequested == true)
                    break;
                if (result.YouTube.Count >= targetCount)
                    break;

                if (IsAlreadyScraped("youtube", url))
                {
                    NoteSkipped();
                    skipped++;
                    Console.WriteLine($"[YouTube] Skip existing: {url}");
                    continue;
                }

                var scrapeResult = await commentScraper.ScrapeCommentsAsync(
                    url, ytMaxComments, browser, _activeOptions);

                if (!scrapeResult.Success)
                {
                    Console.WriteLine($"[YouTube] Skip {url}: {scrapeResult.ErrorMessage}");
                    continue;
                }

                if (!timeFilter.IsWithinRange(scrapeResult.PostedAt, allowUnknownDates))
                {
                    Console.WriteLine($"[YouTube] Skip ngoài khoảng thời gian: {url} posted={scrapeResult.PostedAt}");
                    continue;
                }

                var commentTexts = CommentTextHelper.FromScraped(
                    scrapeResult.Comments, ytMaxComments, "youtube",
                    scrapeResult.Title, scrapeResult.Author);
                if (commentTexts.Count == 0)
                    Console.WriteLine($"[YouTube] Cảnh báo: 0 comment text tại {url}");

                var (feedbackId, savedComments, wasSkipped) = await SaveFeedbackAsync(
                    projectId, "youtube",
                    scrapeResult.Title ?? $"YouTube video: {url}",
                    scrapeResult.Author, url, commentTexts, scrapeResult.PostedAt);

                if (wasSkipped)
                {
                    skipped++;
                    continue;
                }

                lock (result.YouTube)
                {
                    result.YouTube.Add(new PlatformPostResult
                    {
                        FeedbackId = feedbackId,
                        Author = scrapeResult.Author,
                        Text = scrapeResult.Title,
                        Url = url,
                        CommentsCount = savedComments
                    });
                }

                progress?.UpdatePlatform(
                    "youtube",
                    result.YouTube.Count,
                    skipped > 0
                        ? $"YouTube: đã lưu {result.YouTube.Count}/{urls.Count} video (bỏ qua {skipped} cũ)..."
                        : $"YouTube: đã lưu {result.YouTube.Count}/{urls.Count} video...");
            }

            if (result.YouTube.Count == 0 && skipped > 0)
                progress?.CompletePlatform("youtube", 0, $"YouTube: {skipped} video đã cào trước đó — bỏ qua");
            else if (result.YouTube.Count == 0)
            {
                progress?.FailPlatform("youtube", "Tìm thấy video nhưng không lưu được — kiểm tra Playwright.");
                lock (result.Errors)
                    result.Errors.Add("YouTube: Tìm thấy video nhưng không lưu được — kiểm tra Playwright.");
            }
            else
            {
                progress?.CompletePlatform("youtube", result.YouTube.Count,
                    skipped > 0 ? $"Hoàn tất — {result.YouTube.Count} video mới (+{skipped} đã cào trước)" : null);
            }
        }
        catch (Exception ex)
        {
            progress?.FailPlatform("youtube", ex.Message);
            lock (result.Errors)
                result.Errors.Add($"YouTube: {ex.Message}{PlaywrightErrorHint(ex)}");
        }
    }

    private async Task<bool> ProjectEnablesNewsAsync(int projectId) =>
        await _db.DataSources.AsNoTracking()
            .AnyAsync(d => d.ProjectId == projectId
                           && d.Platform == "news"
                           && d.Status == "active");

    private async Task RunNewsAsync(
        int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress,
        ScrapeTimeFilter timeFilter, bool allowUnknownDates)
    {
        var targetCount = _activeOptions.EffectiveMaxNewsArticles;
        var poolSize = timeFilter.DiscoveryPoolSize(targetCount);
        progress?.StartPlatform("news", "Đang tìm bài báo (SerpApi / Google / Bing)...");
        try
        {
            var useProxy = _activeProxy != null;
            List<string> urls;
            try
            {
                urls = await NewsSearchScraper.DiscoverArticleUrlsAsync(
                    keyword,
                    poolSize,
                    _activeOptions,
                    _serpApiNewsDiscovery,
                    msg => progress?.UpdatePlatform("news", result.News.Count, msg),
                    progress?.CancellationToken ?? CancellationToken.None,
                    useProxy ? CurrentPlaywrightProxy() : null);
            }
            catch (Exception ex) when (IsNavigationTimeout(ex) && useProxy)
            {
                Console.WriteLine("[News] Timeout qua proxy — thử lại không dùng proxy...");
                progress?.UpdatePlatform("news", 0, "Tin tức: proxy chậm, thử lại trực tiếp...");
                useProxy = false;
                urls = await NewsSearchScraper.DiscoverArticleUrlsAsync(
                    keyword,
                    poolSize,
                    _activeOptions,
                    _serpApiNewsDiscovery,
                    msg => progress?.UpdatePlatform("news", result.News.Count, msg),
                    progress?.CancellationToken ?? CancellationToken.None,
                    null);
            }

            if (urls.Count == 0)
            {
                progress?.CompletePlatform("news", 0, "Tin tức: không tìm thấy bài");
                lock (result.Errors)
                    result.Errors.Add("Tin tức: Không tìm thấy bài báo cho từ khóa này.");
                return;
            }

            progress?.UpdatePlatform("news", 0, $"Tin tức: tìm thấy {urls.Count} bài, đang đọc nội dung...");

            using var playwright = await Playwright.CreateAsync();
            var newsProxy = useProxy ? CurrentPlaywrightProxy() : null;
            await using var browser = await playwright.Chromium.LaunchAsync(
                PlaywrightScrapeHelper.CreateHeadlessLaunch(_activeOptions.NewsHeadless, newsProxy));

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                Locale = "vi-VN",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7"
                }
            });

            var skipped = 0;
            foreach (var url in urls)
            {
                if (progress?.IsCancellationRequested == true)
                    break;
                if (result.News.Count >= targetCount)
                    break;

                if (IsAlreadyScraped("news", url))
                {
                    NoteSkipped();
                    skipped++;
                    Console.WriteLine($"[News] Skip existing: {url}");
                    continue;
                }

                var article = await NewsSearchScraper.ScrapeArticleAsync(
                    url, _activeOptions, context, progress?.CancellationToken ?? CancellationToken.None);

                if (!article.Success)
                {
                    Console.WriteLine($"[News] Skip {url}: {article.ErrorMessage}");
                    continue;
                }

                if (!timeFilter.IsWithinRange(article.PostedAt, allowUnknownDates))
                {
                    Console.WriteLine($"[News] Skip ngoài khoảng thời gian: {url} posted={article.PostedAt}");
                    continue;
                }

                var content = BuildNewsContent(article.Title, article.Content);
                var (feedbackId, _, wasSkipped) = await SaveFeedbackAsync(
                    projectId, "news",
                    content,
                    article.Author,
                    url,
                    [],
                    article.PostedAt);

                if (wasSkipped)
                {
                    skipped++;
                    continue;
                }

                lock (result.News)
                {
                    result.News.Add(new PlatformPostResult
                    {
                        FeedbackId = feedbackId,
                        Author = article.Author,
                        Text = article.Title,
                        Url = url,
                        CommentsCount = 0
                    });
                }

                Console.WriteLine($"[News] Saved article {feedbackId}: {url}");
                progress?.UpdatePlatform(
                    "news",
                    result.News.Count,
                    skipped > 0
                        ? $"Tin tức: đã lưu {result.News.Count}/{targetCount} bài (bỏ qua {skipped} cũ)..."
                        : $"Tin tức: đã lưu {result.News.Count}/{targetCount} bài...");
            }

            if (result.News.Count == 0 && skipped > 0)
                progress?.CompletePlatform("news", 0, $"Tin tức: {skipped} bài đã cào trước đó — bỏ qua");
            else if (result.News.Count == 0)
            {
                progress?.FailPlatform("news", "Tìm thấy bài nhưng không lưu được — kiểm tra Playwright.");
                lock (result.Errors)
                    result.Errors.Add("Tin tức: Tìm thấy bài nhưng không lưu được — kiểm tra Playwright.");
            }
            else
            {
                progress?.CompletePlatform("news", result.News.Count,
                    skipped > 0 ? $"Hoàn tất — {result.News.Count} bài mới (+{skipped} đã cào trước)" : null);
            }
        }
        catch (Exception ex)
        {
            progress?.FailPlatform("news", ex.Message);
            lock (result.Errors)
                result.Errors.Add($"Tin tức: {ex.Message}{PlaywrightErrorHint(ex)}");
        }
    }

    private static string BuildNewsContent(string? title, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return title?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(title))
            return body.Trim();
        return $"{title.Trim()}\n\n{body.Trim()}";
    }

    private async Task RunTikTokAsync(
        int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress,
        ScrapeTimeFilter timeFilter, bool allowUnknownDates)
    {
        if (progress?.IsCancellationRequested == true)
            return;

        progress?.StartPlatform("tiktok", "Đang tìm video công khai trên TikTok...");

        try
        {
            var captchaTracker = new TikTokCaptchaTracker();
            var headedOnCaptchaOnly = _activeOptions.TikTokHeadedOnCaptchaOnly;
            var initialHeadless = headedOnCaptchaOnly || _activeOptions.TikTokHeadless;

            using var discoveryCts = CreateTikTokSessionCts(
                progress, _activeOptions.EffectiveTikTokDiscoveryTimeoutSeconds);

            var saved = await RunTikTokBrowserSessionAsync(
                projectId, keyword, result, progress, discoveryCts.Token,
                initialHeadless, timeFilter, allowUnknownDates, captchaTracker);

            if (headedOnCaptchaOnly)
            {
                if (captchaTracker.Encountered
                    && _activeOptions.TikTokAllowManualCaptcha
                    && _activeOptions.TikTokCaptchaWaitSeconds > 0
                    && progress?.IsCancellationRequested != true)
                {
                    if (saved > 0 && result.TikTok.Sum(t => t.CommentsCount) == 0)
                        await ClearTikTokPostsWithoutCommentsAsync(projectId, result);

                    progress?.UpdatePlatform("tiktok", result.TikTok.Count,
                        "TikTok: CAPTCHA — mở cửa sổ Chromium, vui lòng giải trong 2 phút...");
                    using var headedCts = CreateTikTokSessionCts(progress, HeadedRetryTimeoutSeconds());
                    saved += await RunTikTokBrowserSessionAsync(
                        projectId, keyword, result, progress, headedCts.Token,
                        headless: false, timeFilter, allowUnknownDates, new TikTokCaptchaTracker());
                }
            }
            else if (saved == 0
                && _activeOptions.TikTokHeadless
                && _activeOptions.EffectiveTikTokRetryHeaded
                && progress?.IsCancellationRequested != true)
            {
                progress?.UpdatePlatform("tiktok", 0,
                    "TikTok: thử lại với cửa sổ Chromium thật (không cần đăng nhập)...");
                using var headedCts = CreateTikTokSessionCts(progress, HeadedRetryTimeoutSeconds());
                saved = await RunTikTokBrowserSessionAsync(
                    projectId, keyword, result, progress, headedCts.Token,
                    headless: false, timeFilter, allowUnknownDates, new TikTokCaptchaTracker());
            }
            else if (_activeOptions.TikTokHeadless
                     && _activeOptions.TikTokRetryHeadedWhenNoComments
                     && _activeOptions.EffectiveTikTokRetryHeaded
                     && progress?.IsCancellationRequested != true
                     && saved > 0
                     && result.TikTok.Sum(t => t.CommentsCount) == 0)
            {
                await ClearTikTokPostsWithoutCommentsAsync(projectId, result);
                progress?.UpdatePlatform("tiktok", 0,
                    "TikTok: có video nhưng 0 comment (CAPTCHA) — thử lại với cửa sổ thật...");
                using var headedCts = CreateTikTokSessionCts(progress, HeadedRetryTimeoutSeconds());
                saved += await RunTikTokBrowserSessionAsync(
                    projectId, keyword, result, progress, headedCts.Token,
                    headless: false, timeFilter, allowUnknownDates, new TikTokCaptchaTracker());
            }

            if (progress?.IsCancellationRequested == true)
                return;

            if (saved == 0 && result.TikTok.Count == 0)
            {
                const string noVideoMsg =
                    "TikTok: không tìm thấy video công khai — thử từ khóa ngắn hơn (vd. FPT).";
                progress?.CompletePlatform("tiktok", 0, noVideoMsg);
                lock (result.Errors)
                    result.Errors.Add(noVideoMsg);
            }
        }
        catch (OperationCanceledException) when (progress?.IsCancellationRequested == true)
        {
            return;
        }
        catch (OperationCanceledException)
        {
            var timeoutMsg =
                $"TikTok: hết thời gian tìm video ({_activeOptions.EffectiveTikTokDiscoveryTimeoutSeconds}s). Thử từ khóa ngắn hơn hoặc bật chế độ demo.";
            progress?.FailPlatform("tiktok", timeoutMsg);
            lock (result.Errors)
                result.Errors.Add(timeoutMsg);
        }
    }

    /// <returns>Số video mới lưu trong phiên này.</returns>
    private async Task<int> RunTikTokBrowserSessionAsync(
        int projectId,
        string keyword,
        ScrapeByKeywordResult result,
        ScrapeJobProgress? progress,
        CancellationToken cancellationToken,
        bool headless,
        ScrapeTimeFilter timeFilter,
        bool allowUnknownDates,
        TikTokCaptchaTracker captchaTracker)
    {
        IBrowserContext? context = null;
        var savedBefore = result.TikTok.Count;
        var targetCount = _activeOptions.EffectiveMaxVideosPerPlatform;
        var poolSize = timeFilter.DiscoveryPoolSize(targetCount);

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                TikTokStealthHelper.CreateLaunchOptions(headless, CurrentPlaywrightProxy()));

            context = await TikTokStealthHelper.CreateContextAsync(browser);

            var urls = await TikTokScraper.DiscoverVideosAsync(
                context,
                keyword,
                poolSize,
                _activeOptions,
                msg => progress?.UpdatePlatform("tiktok", result.TikTok.Count, msg),
                cancellationToken,
                headless,
                captchaTracker);

            if (urls.Count == 0)
            {
                await TikTokSessionHelper.TrySaveAfterSuccessfulSessionAsync(context, captchaTracker, 0);
                return 0;
            }

            progress?.UpdatePlatform("tiktok", result.TikTok.Count,
                $"TikTok: tìm thấy {urls.Count} video, đang lấy nội dung...");

            Console.WriteLine($"[TikTok] Found {urls.Count} video URLs: {string.Join(", ", urls)}");

            var videoScraper = new TikTokScraper();
            var tiktokMaxComments = Math.Max(_activeOptions.EffectiveTikTokMaxComments, _activeOptions.EffectiveMaxCommentsPerItem);
            var skipped = 0;
            var openedVideoCount = 0;

            foreach (var url in urls)
            {
                if (progress?.IsCancellationRequested == true)
                    break;
                if (result.TikTok.Count >= targetCount)
                    break;

                if (IsAlreadyScraped("tiktok", url))
                {
                    NoteSkipped();
                    skipped++;
                    Console.WriteLine($"[TikTok] Skip existing: {url}");
                    continue;
                }

                if (openedVideoCount > 0)
                    await TikTokHumanizeHelper.BetweenVideosAsync(_activeOptions, cancellationToken);
                openedVideoCount++;

                var scrapeResult = await videoScraper.ScrapeVideoAsync(
                    url, tiktokMaxComments, _activeOptions, context, headless, captchaTracker);

                if (!scrapeResult.Success)
                {
                    Console.WriteLine($"[TikTok] Skip {url}: {scrapeResult.ErrorMessage}");
                    continue;
                }

                if (!timeFilter.IsWithinRange(scrapeResult.PostedAt, allowUnknownDates))
                {
                    Console.WriteLine($"[TikTok] Skip ngoài khoảng thời gian: {url} posted={scrapeResult.PostedAt}");
                    continue;
                }

                var commentTexts = CommentTextHelper.FromScraped(
                    scrapeResult.Comments, tiktokMaxComments, "tiktok",
                    scrapeResult.Title, scrapeResult.Author);
                var content = !string.IsNullOrWhiteSpace(scrapeResult.Title)
                    ? scrapeResult.Title!
                    : $"TikTok video: {url}";

                var (feedbackId, savedComments, wasSkipped) = await SaveFeedbackAsync(
                    projectId, "tiktok", content, scrapeResult.Author, url, commentTexts, scrapeResult.PostedAt);

                if (wasSkipped)
                {
                    skipped++;
                    continue;
                }

                lock (result.TikTok)
                {
                    result.TikTok.Add(new PlatformPostResult
                    {
                        FeedbackId = feedbackId,
                        Author = scrapeResult.Author,
                        Text = scrapeResult.Title,
                        Url = url,
                        CommentsCount = savedComments
                    });
                }

                progress?.UpdatePlatform(
                    "tiktok",
                    result.TikTok.Count,
                    skipped > 0
                        ? $"TikTok: đã lưu {result.TikTok.Count}/{urls.Count} video (bỏ qua {skipped} cũ)..."
                        : $"TikTok: đã lưu {result.TikTok.Count}/{urls.Count} video...");
            }

            if (progress?.IsCancellationRequested == true)
            {
                var savedDelta = result.TikTok.Count - savedBefore;
                if (savedDelta > 0)
                    progress?.CompletePlatform("tiktok", result.TikTok.Count,
                        $"Đã lưu {result.TikTok.Count} bài trước khi dừng");
                await TikTokSessionHelper.TrySaveAfterSuccessfulSessionAsync(context, captchaTracker, savedDelta);
                return savedDelta;
            }

            if (result.TikTok.Count > savedBefore)
                progress?.CompletePlatform("tiktok", result.TikTok.Count,
                    skipped > 0 ? $"Hoàn tất — {result.TikTok.Count} video mới (+{skipped} đã cào trước)" : null);
            else if (skipped > 0)
                progress?.CompletePlatform("tiktok", 0, $"TikTok: {skipped} video đã cào trước đó — bỏ qua");
            else if (urls.Count > 0)
                progress?.FailPlatform("tiktok", TikTokCaptchaHelper.PlatformBlockedMessage);

            var newSaved = result.TikTok.Count - savedBefore;
            await TikTokSessionHelper.TrySaveAfterSuccessfulSessionAsync(context, captchaTracker, newSaved);
            return newSaved;
        }
        catch (Exception ex) when (ex is OperationCanceledException && progress?.IsCancellationRequested == true)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            var timeoutMsg =
                $"TikTok: hết thời gian chờ phiên cào ({_activeOptions.EffectiveTikTokDiscoveryTimeoutSeconds}s). Tăng TikTokDiscoveryTimeoutSeconds trong appsettings hoặc thử từ khóa ngắn hơn.";
            progress?.FailPlatform("tiktok", timeoutMsg);
            lock (result.Errors)
                result.Errors.Add(timeoutMsg);
            return result.TikTok.Count - savedBefore;
        }
        catch (Exception ex)
        {
            progress?.FailPlatform("tiktok", ex.Message);
            lock (result.Errors)
                result.Errors.Add($"TikTok: {ex.Message}{PlaywrightErrorHint(ex)}");
            return result.TikTok.Count - savedBefore;
        }
        finally
        {
            if (context != null)
                await context.CloseAsync();
        }
    }

    private async Task ClearTikTokPostsWithoutCommentsAsync(int projectId, ScrapeByKeywordResult result)
    {
        List<PlatformPostResult> zeroCommentPosts;
        lock (result.TikTok)
            zeroCommentPosts = result.TikTok.Where(t => t.CommentsCount == 0).ToList();

        if (zeroCommentPosts.Count == 0)
            return;

        var feedbackIds = zeroCommentPosts
            .Select(t => t.FeedbackId)
            .Where(id => id > 0)
            .ToList();

        if (feedbackIds.Count > 0)
        {
            var analyses = await _db.AiAnalyses
                .Where(a => feedbackIds.Contains(a.FeedbackId))
                .ToListAsync();
            if (analyses.Count > 0)
                _db.AiAnalyses.RemoveRange(analyses);

            var feedbacks = await _db.ScrapedFeedbacks
                .Where(f => feedbackIds.Contains(f.FeedbackId) && f.ProjectId == projectId)
                .ToListAsync();

            foreach (var feedback in feedbacks)
            {
                if (!string.IsNullOrWhiteSpace(feedback.OriginalUrl))
                {
                    var key = ScrapeUrlHelper.DedupeKey("tiktok", feedback.OriginalUrl);
                    if (!string.IsNullOrWhiteSpace(key))
                        _scrapedKeys?.TryRemove(key, out _);
                }

                _db.ScrapedFeedbacks.Remove(feedback);
            }

            await _db.SaveChangesAsync();
        }

        lock (result.TikTok)
            result.TikTok.RemoveAll(t => t.CommentsCount == 0);

        Console.WriteLine($"[TikTok] Đã xóa {zeroCommentPosts.Count} bài 0 comment để cào lại headed.");
    }

    private static CancellationTokenSource CreateTikTokSessionCts(
        ScrapeJobProgress? progress, int timeoutSeconds)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(progress?.CancellationToken ?? default);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 30)));
        return cts;
    }

    private int HeadedRetryTimeoutSeconds()
    {
        var wait = _activeOptions.TikTokAllowManualCaptcha ? _activeOptions.TikTokCaptchaWaitSeconds : 0;
        return _activeOptions.EffectiveTikTokDiscoveryTimeoutSeconds + wait + 120;
    }

    private async Task<List<string>> RunYouTubeSearchAsync(
        YouTubeSearchScraper searchScraper,
        string keyword,
        int poolSize,
        ScrapeTimeFilter timeFilter,
        bool useProxy)
    {
        using var playwright = await Playwright.CreateAsync();
        var proxy = useProxy ? CurrentPlaywrightProxy() : null;
        await using var browser = await playwright.Chromium.LaunchAsync(
            PlaywrightScrapeHelper.YouTubeLaunch(_activeOptions, proxy));
        return await searchScraper.SearchAsync(
            keyword, poolSize, _activeOptions, browser, timeFilter.PostedSinceDays);
    }

    private static bool IsNavigationTimeout(Exception ex) =>
        ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase);

    private static string PlaywrightErrorHint(Exception ex) =>
        ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            ? " (Chưa cài Playwright: playwright.ps1 install chromium)"
            : "";

    private async Task<List<string>> GetFacebookGroupUrlsAsync(int projectId)
    {
        var urls = await _db.FbSources
            .Where(s => s.Status == "active" && s.GroupUrl != null && s.GroupUrl != "")
            .Select(s => s.GroupUrl!)
            .ToListAsync();

        var projectUrls = await _db.DataSources
            .Where(d => d.ProjectId == projectId &&
                        d.TargetUrl != null &&
                        d.TargetUrl != "" &&
                        d.Platform != null &&
                        d.Platform.ToLower() == "facebook")
            .Select(d => d.TargetUrl!)
            .ToListAsync();

        return urls.Concat(projectUrls)
            .Select(FacebookUrlHelper.NormalizeSourceUrl)
            .Where(u => !string.IsNullOrWhiteSpace(u) && FacebookUrlHelper.LooksLikeFacebookUrl(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<(int FeedbackId, int SavedComments, bool Skipped)> SaveFeedbackAsync(
        int projectId, string platform, string content, string? authorName, string originalUrl,
        List<string> comments, DateTime? postedAt = null)
    {
        await _saveLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(originalUrl) && IsAlreadyScraped(platform, originalUrl))
            {
                NoteSkipped();
                Console.WriteLine($"[{platform}] Skip trùng URL: {originalUrl}");
                return (0, 0, true);
            }

            var normalized = CommentTextHelper.Normalize(comments);

            var feedback = new ScrapedFeedback
            {
                SourceId = null,
                ProjectId = projectId,
                Platform = platform,
                Content = content,
                AuthorName = authorName,
                OriginalUrl = originalUrl,
                PostedAt = postedAt,
                CommentsCount = normalized.Count,
                ScrapedAt = DateTime.Now
            };

            _db.ScrapedFeedbacks.Add(feedback);
            await _db.SaveChangesAsync();

            var savedCount = await _bundleStorage.SaveAsync(feedback.FeedbackId, normalized);
            feedback.CommentsCount = savedCount;
            feedback.CommentsFileUrl = CommentBundleStorage.GetRelativeBundlePath(feedback.FeedbackId);
            await _db.SaveChangesAsync();

            MarkScraped(platform, originalUrl);

            Console.WriteLine($"[{platform}] Feedback {feedback.FeedbackId}: lưu {savedCount} comment text");

            return (feedback.FeedbackId, savedCount, false);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
