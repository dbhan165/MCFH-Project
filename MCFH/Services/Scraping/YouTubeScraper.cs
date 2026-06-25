using Microsoft.Playwright;
using MCFH.Configuration;
using MCFH.Models.Scraping;
using System.Text.RegularExpressions;
using System.Web;

namespace MCFH.Services.Scraping;

public class YouTubeScraper
{
    private readonly ILogger<YouTubeScraper> _logger;

    public YouTubeScraper(ILogger<YouTubeScraper> logger)
    {
        _logger = logger;
    }

    public static string GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";

        if (url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(url.Split('?')[0]);
            return uri.AbsolutePath.TrimStart('/').Split('/')[0];
        }

        var parsed = new Uri(url);
        var query = HttpUtility.ParseQueryString(parsed.Query);
        return query["v"] ?? "";
    }

    public async Task<ScrapeResult> ScrapeCommentsAsync(
        string videoUrl,
        int maxComments = 50,
        IBrowser? sharedBrowser = null,
        ScrapeOptions? options = null)
    {
        var result = new ScrapeResult();
        var ownsBrowser = sharedBrowser == null;
        IPlaywright? playwright = null;
        IBrowser? browser = sharedBrowser;

        try
        {
            _logger.LogInformation("[YouTube] Bắt đầu cào: {Url}", videoUrl);

            if (browser == null)
            {
                playwright = await Playwright.CreateAsync();
                var launchOpts = options != null
                    ? PlaywrightScrapeHelper.YouTubeLaunch(options)
                    : PlaywrightScrapeHelper.CreateHeadlessLaunch(true);
                browser = await playwright.Chromium.LaunchAsync(launchOpts);
            }

            var page = await browser.NewPageAsync();
            try
            {
                await ScrapeCommentsOnPageAsync(page, videoUrl, maxComments, result);
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[YouTube] Lỗi khi cào {Url}", videoUrl);
        }
        finally
        {
            if (ownsBrowser && browser != null)
                await browser.DisposeAsync();
            playwright?.Dispose();
        }

        return result;
    }

    private async Task ScrapeCommentsOnPageAsync(IPage page, string videoUrl, int maxComments, ScrapeResult result)
    {
        var cleanUrl = NormalizeVideoUrl(videoUrl);
        var originalVideoId = GetVideoId(cleanUrl);

        page.FrameNavigated += (_, e) =>
        {
            if (e.Url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase)
                && GetVideoId(e.Url) != originalVideoId)
            {
                _logger.LogWarning("[YouTube] Chặn chuyển sang video khác");
                _ = page.GotoAsync(cleanUrl);
            }
        };

        await page.GotoAsync(cleanUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45000
        });

        await DismissConsentAsync(page);
        await SkipAdsAsync(page);
        await page.WaitForTimeoutAsync(1500);

        result.Title = (await page.TitleAsync()).Replace(" - YouTube", "", StringComparison.OrdinalIgnoreCase).Trim();
        result.Author = await TryGetChannelNameAsync(page);

        await page.EvaluateAsync(@"() => {
            const v = document.querySelector('video');
            if (v) { v.pause(); v.autoplay = false; }
        }");

        await OpenCommentsSectionAsync(page);

        var collected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var text in await ExtractCommentsFromYtInitialDataAsync(page))
            collected.Add(text);

        await ScrollCommentsAsync(page, maxComments);
        foreach (var text in await ExtractCommentsFromDomAsync(page))
            collected.Add(text);

        if (collected.Count < Math.Min(5, maxComments / 2))
        {
            await TrySortTopCommentsAsync(page);
            await ScrollCommentsAsync(page, maxComments, aggressive: true);
            await ExpandYouTubeRepliesAsync(page);
            foreach (var text in await ExtractCommentsFromYtInitialDataAsync(page))
                collected.Add(text);
            foreach (var text in await ExtractCommentsFromDomAsync(page))
                collected.Add(text);
        }

        var normalized = CommentTextHelper.FilterYouTube(
            collected, maxComments, result.Title, result.Author);
        foreach (var text in normalized)
        {
            result.Comments.Add(new ScrapedComment
            {
                Author = "",
                Text = text,
                Source = "youtube"
            });
        }

        result.Success = !string.IsNullOrWhiteSpace(result.Title) || result.Comments.Count > 0;
        result.TotalScraped = result.Comments.Count;
        _logger.LogInformation(
            "[YouTube] Xong '{Title}' — {Count} comments",
            Truncate(result.Title, 40),
            result.TotalScraped);

        if (!result.Success)
            result.ErrorMessage = "Không lấy được tiêu đề hoặc bình luận YouTube.";
    }

    private static string NormalizeVideoUrl(string url)
    {
        var clean = url.Trim();
        if (!clean.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            clean = "https://" + clean;

        var id = GetVideoId(clean);
        return string.IsNullOrEmpty(id) ? clean : $"https://www.youtube.com/watch?v={id}";
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";

    private static async Task DismissConsentAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "button[aria-label*='Accept']",
            "button[aria-label*='Chấp nhận']",
            "button:has-text('Accept all')",
            "button:has-text('Từ chối')",
            "button:has-text('Reject all')",
            "tp-yt-paper-button:has-text('Từ chối')",
            "tp-yt-paper-button:has-text('Reject')"
        })
        {
            try
            {
                var btn = page.Locator(selector).First;
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                    await page.WaitForTimeoutAsync(800);
                    return;
                }
            }
            catch { }
        }
    }

    private async Task SkipAdsAsync(IPage page)
    {
        for (var i = 0; i < 10; i++)
        {
            var isAdShowing = await page.EvalOnSelectorAsync<bool>(
                ".html5-video-player",
                "el => el.classList.contains('ad-showing')");

            if (!isAdShowing) break;

            var skipButton = page.Locator(".ytp-ad-skip-button, .ytp-skip-ad-button, .ytp-ad-skip-button-modern");
            if (await skipButton.IsVisibleAsync())
                await skipButton.ClickAsync();

            await page.WaitForTimeoutAsync(500);
        }
    }

    private static async Task<string?> TryGetChannelNameAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "ytd-channel-name yt-formatted-string a",
            "ytd-video-owner-renderer #channel-name a",
            "#owner #channel-name yt-formatted-string",
            "yt-formatted-string.ytd-channel-name"
        })
        {
            try
            {
                var el = page.Locator(selector).First;
                if (await el.CountAsync() > 0)
                {
                    var text = (await el.InnerTextAsync()).Trim();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            catch { }
        }

        return null;
    }

    private static async Task OpenCommentsSectionAsync(IPage page)
    {
        try
        {
            await page.EvaluateAsync(@"() => {
                const targets = [
                    document.querySelector('ytd-comments#comments'),
                    document.querySelector('#comments'),
                    document.querySelector('ytd-item-section-renderer#sections')
                ];
                for (const el of targets) {
                    if (el) { el.scrollIntoView({ block: 'start' }); break; }
                }
                window.scrollTo(0, 700);
            }");
            await page.WaitForTimeoutAsync(2000);

            await page.Locator("ytd-comments#comments, #comments, ytd-comment-thread-renderer")
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 12000, State = WaitForSelectorState.Attached });
        }
        catch { }
    }

    private static async Task ScrollCommentsAsync(IPage page, int maxComments, bool aggressive = false)
    {
        var iterations = aggressive ? 18 : 12;
        var previousCount = 0;
        var noChangeStreak = 0;

        for (var i = 0; i < iterations; i++)
        {
            var currentCount = await page.Locator(
                "ytd-comment-thread-renderer, ytd-comment-view-model, ytd-comment-renderer").CountAsync();

            if (currentCount >= maxComments) break;

            if (currentCount == previousCount)
            {
                noChangeStreak++;
                if (noChangeStreak >= 4) break;
            }
            else noChangeStreak = 0;

            previousCount = currentCount;

            await page.EvaluateAsync(@"() => {
                const panel = document.querySelector('ytd-comments#comments #contents')
                    || document.querySelector('#contents.style-scope.ytd-comments');
                if (panel) panel.scrollTop += 900;
                window.scrollBy(0, 900);
            }");

            await page.Keyboard.PressAsync("End");
            await page.WaitForTimeoutAsync(aggressive ? 1100 : 900);
        }
    }

    private static async Task TrySortTopCommentsAsync(IPage page)
    {
        try
        {
            var sortBtn = page.Locator(
                "tp-yt-paper-button#sort-menu, button[aria-label*='Sort'], button:has-text('Sort by')").First;
            if (await sortBtn.IsVisibleAsync())
            {
                await sortBtn.ClickAsync();
                await page.WaitForTimeoutAsync(500);
                var top = page.Locator("tp-yt-paper-item:has-text('Top'), yt-sort-filter-renderer:has-text('Top')").First;
                if (await top.IsVisibleAsync())
                    await top.ClickAsync();
                await page.WaitForTimeoutAsync(800);
            }
        }
        catch { }
    }

    private static async Task ExpandYouTubeRepliesAsync(IPage page)
    {
        try
        {
            var buttons = page.Locator("ytd-button-renderer button, button#button")
                .Filter(new LocatorFilterOptions
                {
                    HasTextRegex = new Regex(
                        @"\d+ repl|phản hồi|replies|View \d+",
                        RegexOptions.IgnoreCase)
                });
            var count = Math.Min(await buttons.CountAsync(), 8);
            for (var i = 0; i < count; i++)
            {
                try
                {
                    await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    await page.WaitForTimeoutAsync(600);
                }
                catch { }
            }
        }
        catch { }
    }

    private static async Task<List<string>> ExtractCommentsFromDomAsync(IPage page)
    {
        return (await page.EvaluateAsync<string[]>(@"
            () => {
                const texts = new Set();
                const add = (t) => {
                    if (typeof t === 'string' && t.trim().length > 1 && t.trim().length < 2000)
                        texts.add(t.trim());
                };

                const selectors = [
                    'ytd-comment-thread-renderer #content-text',
                    'ytd-comment-view-model #content-text',
                    'ytd-comment-renderer #content-text',
                    'yt-formatted-string#content-text'
                ];

                const isInsideComment = (el) => {
                    return el.closest('ytd-comment-thread-renderer')
                        || el.closest('ytd-comment-view-model')
                        || el.closest('ytd-comment-renderer');
                };

                for (const sel of selectors) {
                    document.querySelectorAll(sel).forEach(el => {
                        if (!isInsideComment(el)) return;
                        add(el.textContent);
                    });
                }

                return Array.from(texts);
            }
        ")).ToList();
    }

    private static async Task<List<string>> ExtractCommentsFromYtInitialDataAsync(IPage page)
    {
        try
        {
            return (await page.EvaluateAsync<string[]>(@"
                () => {
                    const texts = new Set();
                    const add = (t) => {
                        if (typeof t !== 'string') return;
                        const v = t.replace(/\s+/g, ' ').trim();
                        if (v.length < 2 || v.length > 2000) return;
                        if (/^(Reply|Phản hồi|\d+ replies?|\d+ phản hồi|Pinned by|Được ghim)/i.test(v)) return;
                        texts.add(v);
                    };

                    const fromRuns = (runs) => {
                        if (!Array.isArray(runs)) return;
                        const t = runs.map(r => r.text || '').join('');
                        add(t);
                    };

                    const walk = (obj, depth) => {
                        if (!obj || typeof obj !== 'object' || depth > 30) return;
                        if (Array.isArray(obj)) {
                            obj.forEach(v => walk(v, depth + 1));
                            return;
                        }

                        if (obj.commentRenderer?.contentText?.runs)
                            fromRuns(obj.commentRenderer.contentText.runs);

                        if (obj.contentText?.runs && (obj.commentRenderer || obj.cid))
                            fromRuns(obj.contentText.runs);

                        if (obj.commentViewModel?.content?.content)
                            add(obj.commentViewModel.content.content);

                        if (obj.properties?.content?.content && obj.commentViewModel)
                            add(obj.properties.content.content);

                        Object.values(obj).forEach(v => walk(v, depth + 1));
                    };

                    if (window.ytInitialData) walk(window.ytInitialData, 0);

                    const scripts = document.querySelectorAll('script');
                    for (const s of scripts) {
                        const txt = s.textContent || '';
                        if (!txt.includes('commentRenderer') && !txt.includes('commentViewModel')) continue;
                        const m = txt.match(/ytInitialData\s*=\s*(\{.+?\});/s);
                        if (m) {
                            try { walk(JSON.parse(m[1]), 0); } catch {}
                        }
                    }

                    return Array.from(texts);
                }
            ")).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}
