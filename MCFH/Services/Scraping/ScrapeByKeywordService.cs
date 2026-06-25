using System.Collections.Concurrent;
using MCFH.Configuration;
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
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private ConcurrentDictionary<string, byte>? _scrapedKeys;
    private int _skippedCount;

    public ScrapeByKeywordService(
        McfhDbContext db,
        AiAnalysisService aiAnalysisService,
        IOptions<ScrapeOptions> scrapeOptions)
    {
        _db = db;
        _aiAnalysisService = aiAnalysisService;
        _scrapeOptions = scrapeOptions.Value;
    }

    public async Task<ScrapeByKeywordResult> ScrapeAsync(int projectId, ScrapeJobProgress? progress = null)
    {
        var result = new ScrapeByKeywordResult();

        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
        {
            result.ErrorMessage = "Project không tồn tại.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(project.SearchQuery))
        {
            result.ErrorMessage = "Project chưa setup keyword (search_query rỗng).";
            return result;
        }

        var keyword = project.SearchQuery;
        result.Keyword = keyword;

        progress?.InitPlatforms(
            project.EnableFacebook == true,
            project.EnableYoutube == true,
            project.EnableTiktok == true);
        progress?.SetPhase("scraping", $"Đang cào từ khóa «{keyword}»...");

        _scrapedKeys = await LoadExistingScrapeKeysAsync(projectId);
        _skippedCount = 0;

        try
        {
            var parallelTasks = new List<Task>();

            if (project.EnableFacebook == true)
                parallelTasks.Add(RunFacebookAsync(projectId, keyword, result, progress));

            if (project.EnableYoutube == true)
                parallelTasks.Add(RunYouTubeAsync(projectId, keyword, result, progress));

            if (parallelTasks.Count == 0 && project.EnableTiktok != true)
            {
                result.ErrorMessage = "Project chưa bật nguồn dữ liệu nào.";
                return result;
            }

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

            if (project.EnableTiktok == true)
                await RunTikTokAsync(projectId, keyword, result, progress);

            return await FinalizeResultAsync(projectId, result, progress, progress?.IsCancellationRequested == true);
        }
        finally
        {
            _scrapedKeys = null;
            _skippedCount = 0;
        }
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
        var totalSaved = result.Facebook.Count + result.YouTube.Count + result.TikTok.Count;

        if (cancelled)
        {
            progress?.SkipPendingPlatforms("Đã bỏ qua — người dùng dừng");
            result.Message = totalSaved > 0
                ? $"Đã dừng — giữ {totalSaved} bài (FB: {result.Facebook.Count}, YT: {result.YouTube.Count}, TT: {result.TikTok.Count})."
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
                $"Đã lưu {totalSaved} bản ghi + {totalComments} bình luận text (FB: {result.Facebook.Count}, YT: {result.YouTube.Count}, TT: {result.TikTok.Count}).";

            progress?.SetPhase("analyzing", "Đang phân tích AI các bài vừa cào...");
            result.Analysis = await _aiAnalysisService.AnalyzePendingFeedbacksAsync(projectId, force: false);
            if (result.Analysis.AnalyzedCount > 0)
                result.Message += $" {result.Analysis.Message}";
        }

        return result;
    }

    private async Task RunFacebookAsync(int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress)
    {
        var facebookUrls = await GetFacebookGroupUrlsAsync(projectId);
        if (facebookUrls.Count == 0)
        {
            progress?.FailPlatform("facebook", "Chưa có group URL trong FB_SOURCES hoặc data sources.");
            lock (result.Errors)
                result.Errors.Add("Facebook: Chưa có group URL trong FB_SOURCES hoặc data sources.");
            return;
        }

        progress?.StartPlatform("facebook", "Đang tìm bài trên Facebook...");
        var skipped = 0;

        foreach (var groupUrl in facebookUrls)
        {
            if (progress?.IsCancellationRequested == true)
                break;

            try
            {
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var searchUrl = $"{groupUrl.TrimEnd('/')}/search/?q={encodedKeyword}";

                var scraper = new FacebookGroupScraper();
                var posts = await scraper.ScrapeAsync(searchUrl, _scrapeOptions.MaxFacebookPosts, _scrapeOptions);

                foreach (var post in posts)
                {
                    if (progress?.IsCancellationRequested == true)
                        break;

                    var postUrl = post.PostUrl ?? "";
                    if (IsAlreadyScraped("facebook", postUrl))
                    {
                        NoteSkipped();
                        skipped++;
                        Console.WriteLine($"[Facebook] Skip existing: {postUrl}");
                        continue;
                    }

                    var fbMax = Math.Max(_scrapeOptions.FacebookMaxComments, _scrapeOptions.MaxCommentsPerItem);
                    var commentTexts = CommentTextHelper.FilterFacebook(post.Comments, fbMax);

                    if (commentTexts.Count < 3 && !string.IsNullOrWhiteSpace(post.PostUrl))
                    {
                        var fbCommentScraper = new FacebookCommentScraper();
                        var extra = await fbCommentScraper.ScrapePostCommentsAsync(post.PostUrl!, _scrapeOptions);
                        commentTexts = CommentTextHelper.FilterFacebook(commentTexts.Concat(extra), fbMax);
                    }

                    var (feedbackId, savedComments, wasSkipped) = await SaveFeedbackAsync(
                        projectId, "facebook", post.Text, post.Author, postUrl, commentTexts);

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

    private async Task RunYouTubeAsync(int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress)
    {
        progress?.StartPlatform("youtube", "Đang tìm video trên YouTube...");
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                PlaywrightScrapeHelper.YouTubeLaunch(_scrapeOptions));

            var searchScraper = new YouTubeSearchScraper();
            var urls = await searchScraper.SearchAsync(
                keyword, _scrapeOptions.MaxVideosPerPlatform, _scrapeOptions, browser);

            var commentScraper = new YouTubeScraper(NullLoggerFactory.Instance.CreateLogger<YouTubeScraper>());
            var ytMaxComments = Math.Max(_scrapeOptions.YouTubeMaxComments, _scrapeOptions.MaxCommentsPerItem);

            if (urls.Count == 0)
            {
                progress?.CompletePlatform("youtube", 0, "YouTube: không tìm thấy video");
                lock (result.Errors)
                    result.Errors.Add("YouTube: Không tìm thấy video cho từ khóa này.");
                return;
            }

            progress?.UpdatePlatform("youtube", 0, $"YouTube: tìm thấy {urls.Count} video, đang lấy comment...");

            Console.WriteLine($"[YouTube] Found {urls.Count} video(s)");

            var skipped = 0;
            foreach (var url in urls)
            {
                if (progress?.IsCancellationRequested == true)
                    break;

                if (IsAlreadyScraped("youtube", url))
                {
                    NoteSkipped();
                    skipped++;
                    Console.WriteLine($"[YouTube] Skip existing: {url}");
                    continue;
                }

                var scrapeResult = await commentScraper.ScrapeCommentsAsync(
                    url, ytMaxComments, browser, _scrapeOptions);

                if (!scrapeResult.Success)
                {
                    Console.WriteLine($"[YouTube] Skip {url}: {scrapeResult.ErrorMessage}");
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
                    scrapeResult.Author, url, commentTexts);

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

    private async Task RunTikTokAsync(int projectId, string keyword, ScrapeByKeywordResult result, ScrapeJobProgress? progress)
    {
        if (progress?.IsCancellationRequested == true)
            return;

        progress?.StartPlatform("tiktok", "Đang tìm video công khai trên TikTok...");

        using var discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(progress?.CancellationToken ?? default);
        discoveryCts.CancelAfter(TimeSpan.FromSeconds(_scrapeOptions.TikTokDiscoveryTimeoutSeconds));

        try
        {
            var saved = await RunTikTokBrowserSessionAsync(
                projectId, keyword, result, progress, discoveryCts.Token, _scrapeOptions.TikTokHeadless);

            if (saved == 0
                && _scrapeOptions.TikTokHeadless
                && _scrapeOptions.TikTokRetryHeadedOnFailure
                && progress?.IsCancellationRequested != true)
            {
                progress?.UpdatePlatform("tiktok", 0,
                    "TikTok: thử lại với cửa sổ Chromium thật (không cần đăng nhập)...");
                saved = await RunTikTokBrowserSessionAsync(
                    projectId, keyword, result, progress, discoveryCts.Token, headless: false);
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
                $"TikTok: hết thời gian tìm video ({_scrapeOptions.TikTokDiscoveryTimeoutSeconds}s). Thử từ khóa ngắn hơn.";
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
        bool headless)
    {
        IBrowserContext? context = null;
        var savedBefore = result.TikTok.Count;

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                TikTokStealthHelper.CreateLaunchOptions(headless));

            context = await TikTokStealthHelper.CreateContextAsync(browser);

            var urls = await TikTokScraper.DiscoverVideosAsync(
                context,
                keyword,
                _scrapeOptions.MaxVideosPerPlatform,
                _scrapeOptions,
                msg => progress?.UpdatePlatform("tiktok", result.TikTok.Count, msg),
                cancellationToken);

            if (urls.Count == 0)
                return 0;

            progress?.UpdatePlatform("tiktok", result.TikTok.Count,
                $"TikTok: tìm thấy {urls.Count} video, đang lấy nội dung...");

            Console.WriteLine($"[TikTok] Found {urls.Count} video URLs: {string.Join(", ", urls)}");

            var videoScraper = new TikTokScraper();
            var tiktokMaxComments = Math.Max(_scrapeOptions.TikTokMaxComments, _scrapeOptions.MaxCommentsPerItem);
            var skipped = 0;

            foreach (var url in urls)
            {
                if (progress?.IsCancellationRequested == true)
                    break;

                if (IsAlreadyScraped("tiktok", url))
                {
                    NoteSkipped();
                    skipped++;
                    Console.WriteLine($"[TikTok] Skip existing: {url}");
                    continue;
                }

                var scrapeResult = await videoScraper.ScrapeVideoAsync(
                    url, tiktokMaxComments, _scrapeOptions, context);

                if (!scrapeResult.Success)
                {
                    Console.WriteLine($"[TikTok] Skip {url}: {scrapeResult.ErrorMessage}");
                    continue;
                }

                var commentTexts = CommentTextHelper.FromScraped(
                    scrapeResult.Comments, tiktokMaxComments, "tiktok",
                    scrapeResult.Title, scrapeResult.Author);
                var content = !string.IsNullOrWhiteSpace(scrapeResult.Title)
                    ? scrapeResult.Title!
                    : $"TikTok video: {url}";

                var (feedbackId, savedComments, wasSkipped) = await SaveFeedbackAsync(
                    projectId, "tiktok", content, scrapeResult.Author, url, commentTexts);

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
                if (result.TikTok.Count > savedBefore)
                    progress?.CompletePlatform("tiktok", result.TikTok.Count,
                        $"Đã lưu {result.TikTok.Count} bài trước khi dừng");
                return result.TikTok.Count - savedBefore;
            }

            if (result.TikTok.Count > savedBefore)
                progress?.CompletePlatform("tiktok", result.TikTok.Count,
                    skipped > 0 ? $"Hoàn tất — {result.TikTok.Count} video mới (+{skipped} đã cào trước)" : null);
            else if (skipped > 0)
                progress?.CompletePlatform("tiktok", 0, $"TikTok: {skipped} video đã cào trước đó — bỏ qua");
            else if (urls.Count > 0)
                progress?.FailPlatform("tiktok", TikTokCaptchaHelper.PlatformBlockedMessage);

            await TikTokSessionHelper.SaveCookiesAsync(context);
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

    private static string PlaywrightErrorHint(Exception ex) =>
        ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            ? " (Chưa cài Playwright: playwright.ps1 install chromium)"
            : "";

    private async Task<List<string>> GetFacebookGroupUrlsAsync(int projectId)
    {
        var urls = await _db.FbSources
            .Where(s => s.Status == "active" && s.GroupUrl != null && s.GroupUrl != "")
            .Select(s => s.GroupUrl)
            .ToListAsync();

        var projectUrls = await _db.DataSources
            .Where(d => d.ProjectId == projectId &&
                        d.Platform == "facebook" &&
                        d.TargetUrl != null &&
                        d.TargetUrl != "")
            .Select(d => d.TargetUrl!)
            .ToListAsync();

        return urls.Concat(projectUrls).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<(int FeedbackId, int SavedComments, bool Skipped)> SaveFeedbackAsync(
        int projectId, string platform, string content, string? authorName, string originalUrl, List<string> comments)
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
                CommentsCount = normalized.Count,
                ScrapedAt = DateTime.Now
            };

            _db.ScrapedFeedbacks.Add(feedback);
            await _db.SaveChangesAsync();

            var savedCount = await CommentBundleStorage.SaveAsync(feedback.FeedbackId, normalized);
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
