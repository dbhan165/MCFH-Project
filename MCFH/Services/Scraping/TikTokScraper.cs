using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public class TikTokScraper
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<ScrapeResult> ScrapeVideoAsync(
        string videoUrl,
        int maxComments,
        ScrapeOptions options,
        IBrowserContext? sharedContext = null)
    {
        var result = new ScrapeResult();
        var ownsContext = sharedContext == null;
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        IBrowserContext? context = sharedContext;

        try
        {
            var cleanUrl = NormalizeVideoUrl(videoUrl);
            await TryOEmbedAsync(cleanUrl, result);
            TikTokUrlDiscovery.ParseAuthorFromUrl(cleanUrl, result);

            if (context == null)
            {
                playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync(
                    TikTokStealthHelper.CreateLaunchOptions(options.TikTokHeadless));
                context = await TikTokStealthHelper.CreateContextAsync(browser);
            }

            var page = await context.NewPageAsync();
            try
            {
                await ScrapeVideoOnPageAsync(page, cleanUrl, maxComments, result, options);
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
            Console.WriteLine($"[TikTok] Error: {ex.Message}");
        }
        finally
        {
            if (ownsContext && context != null)
                await context.CloseAsync();
            if (browser != null)
                await browser.DisposeAsync();
            playwright?.Dispose();
        }

        return result;
    }

    public static async Task<List<string>> DiscoverVideosAsync(
        IBrowserContext context,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default)
    {
        var page = await context.NewPageAsync();
        try
        {
            return await TikTokUrlDiscovery.DiscoverAsync(
                page, keyword, maxVideos, options, onStatus, cancellationToken);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static string NormalizeVideoUrl(string url)
    {
        var clean = url.Split('?')[0].Split('#')[0].Trim();
        if (clean.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            clean = "https://" + clean["http://".Length..];
        return clean.Replace("https://m.tiktok.com", "https://www.tiktok.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task TryOEmbedAsync(string videoUrl, ScrapeResult result)
    {
        try
        {
            var apiUrl =
                $"https://www.tiktok.com/oembed?url={Uri.EscapeDataString(videoUrl)}";
            var payload = await Http.GetFromJsonAsync<OEmbedResponse>(apiUrl);
            if (payload == null) return;

            if (string.IsNullOrWhiteSpace(result.Title) && !string.IsNullOrWhiteSpace(payload.Title))
                result.Title = payload.Title;

            if (string.IsNullOrWhiteSpace(result.Author) && !string.IsNullOrWhiteSpace(payload.AuthorName))
                result.Author = payload.AuthorName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok] oEmbed skip: {ex.Message}");
        }
    }

    public static async Task ScrapeVideoOnPageAsync(
        IPage page,
        string videoUrl,
        int maxComments,
        ScrapeResult result,
        ScrapeOptions options)
    {
        var cleanUrl = NormalizeVideoUrl(videoUrl);
        Console.WriteLine($"[TikTok] Opening video: {cleanUrl}");

        TikTokUrlDiscovery.ParseAuthorFromUrl(cleanUrl, result);

        var capture = new TikTokNetworkCapture();
        capture.Attach(page, maxComments);

        await page.GotoAsync(cleanUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000
        });
        await TikTokStealthHelper.DismissCookieBannersAsync(page);

        var captchaBlocked = !await TikTokCaptchaHelper.TryContinueAsync(page, options, "video");
        await page.WaitForTimeoutAsync(2500);

        await FillFromDomAsync(page, result);
        await FillFromEmbeddedJsonAsync(page, result);

        var collectedComments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!captchaBlocked)
        {
            await TryActivateCommentsTabAsync(page);
            await TryOpenCommentsAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await ExpandReplyThreadsAsync(page);
            await ScrollCommentsAsync(page, maxComments);

            foreach (var text in await ExtractCommentsFromDomAsync(page))
                collectedComments.Add(text);

            foreach (var text in await ExtractCommentsFromJsonAsync(page))
                collectedComments.Add(text);

            foreach (var text in capture.Comments)
                collectedComments.Add(text);

            if (collectedComments.Count < maxComments / 2)
            {
                await ExpandReplyThreadsAsync(page);
                await ScrollCommentsAsync(page, maxComments, aggressive: true);
                foreach (var text in await ExtractCommentsFromDomAsync(page))
                    collectedComments.Add(text);
                foreach (var text in capture.Comments)
                    collectedComments.Add(text);
            }
        }
        else
        {
            foreach (var text in capture.Comments)
                collectedComments.Add(text);
        }

        var filtered = CommentTextHelper.FilterTikTok(
            collectedComments, maxComments, result.Title, result.Author);

        foreach (var text in filtered)
        {
            result.Comments.Add(new ScrapedComment
            {
                Author = "",
                Text = text,
                Source = "tiktok"
            });
        }

        if (string.IsNullOrWhiteSpace(result.Title))
        {
            result.Title = await page.EvaluateAsync<string?>(@"
                () => document.querySelector('meta[property=""og:description""]')?.getAttribute('content')
                   || document.querySelector('meta[name=""description""]')?.getAttribute('content') || ''
            ") ?? "";
        }

        if (string.IsNullOrWhiteSpace(result.Author))
        {
            result.Author = await page.EvaluateAsync<string?>(@"
                () => document.querySelector('meta[property=""og:title""]')?.getAttribute('content') || ''
            ");
        }

        if (string.IsNullOrWhiteSpace(result.Title) && !string.IsNullOrWhiteSpace(result.Author))
            result.Title = $"Video TikTok của {result.Author}";

        if (string.IsNullOrWhiteSpace(result.Title))
            result.Title = $"TikTok video {cleanUrl}";

        var hasValidUrl = Regex.IsMatch(cleanUrl, @"tiktok\.com/@[^/]+/video/\d+", RegexOptions.IgnoreCase);
        if (!hasValidUrl)
        {
            result.Success = false;
            result.ErrorMessage = "URL video TikTok không hợp lệ.";
            return;
        }

        if (captchaBlocked && collectedComments.Count == 0 && string.IsNullOrWhiteSpace(result.Title))
        {
            result.Success = false;
            result.ErrorMessage = TikTokCaptchaHelper.PlatformBlockedMessage;
            return;
        }

        result.Success = true;
        result.TotalScraped = result.Comments.Count;
        if (captchaBlocked && result.Comments.Count == 0)
            Console.WriteLine("[TikTok] CAPTCHA — lưu tiêu đề/mô tả video, không có comment.");

        Console.WriteLine($"[TikTok] OK title='{Truncate(result.Title, 50)}' comments={result.TotalScraped}");
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";

    private static async Task FillFromDomAsync(IPage page, ScrapeResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Title))
        {
            result.Title = await TryGetTextAsync(page,
                "[data-e2e='browse-video-desc']",
                "[data-e2e='video-desc']",
                "h1[data-e2e='video-desc']",
                "[data-e2e='video-desc'] span") ?? "";
        }

        if (string.IsNullOrWhiteSpace(result.Author))
        {
            result.Author = await TryGetTextAsync(page,
                "[data-e2e='browse-username']",
                "[data-e2e='video-author-uniqueid']",
                "h2[data-e2e='browse-username']") ?? "";
        }
    }

    private static async Task FillFromEmbeddedJsonAsync(IPage page, ScrapeResult result)
    {
        try
        {
            var jsonText = await page.EvaluateAsync<string?>(@"
                () => {
                    const el = document.querySelector('#SIGI_STATE')
                        || document.querySelector('script[id=""__UNIVERSAL_DATA_FOR_REHYDRATION__""]');
                    return el ? el.textContent : null;
                }
            ");

            if (string.IsNullOrWhiteSpace(jsonText)) return;

            using var doc = JsonDocument.Parse(jsonText);
            WalkJson(doc.RootElement, result);
        }
        catch { }
    }

    private static void WalkJson(JsonElement element, ScrapeResult result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("desc", out var desc) && desc.ValueKind == JsonValueKind.String
                    && string.IsNullOrWhiteSpace(result.Title))
                    result.Title = desc.GetString();

                if (element.TryGetProperty("uniqueId", out var uniqueId) && uniqueId.ValueKind == JsonValueKind.String
                    && string.IsNullOrWhiteSpace(result.Author))
                    result.Author = uniqueId.GetString();

                if (element.TryGetProperty("nickname", out var nickname) && nickname.ValueKind == JsonValueKind.String
                    && string.IsNullOrWhiteSpace(result.Author))
                    result.Author = nickname.GetString();

                foreach (var prop in element.EnumerateObject())
                    WalkJson(prop.Value, result);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    WalkJson(item, result);
                break;
        }
    }

    private static async Task<string?> TryGetTextAsync(IPage page, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector);
                if (await locator.CountAsync() == 0) continue;
                var text = (await locator.First.InnerTextAsync()).Trim();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch { }
        }
        return null;
    }

    private static async Task<List<string>> ExtractCommentsFromDomAsync(IPage page)
    {
        return (await page.EvaluateAsync<string[]>(@"
            () => {
                const texts = [];
                const seen = new Set();
                const add = (t) => {
                    const v = (t || '').replace(/\s+/g, ' ').trim();
                    if (v.length < 3 || v.length > 2000) return;
                    const key = v.toLowerCase();
                    if (seen.has(key)) return;
                    seen.add(key);
                    texts.push(v);
                };

                const pickFromItem = (item) => {
                    const textEl = item.querySelector('[data-e2e=""comment-text""]');
                    if (textEl) {
                        add(textEl.textContent);
                        return;
                    }
                    const spans = item.querySelectorAll('span[dir=""auto""], p[dir=""auto""]');
                    for (const span of spans) {
                        const t = (span.textContent || '').trim();
                        if (t.length < 3) continue;
                        if (/^(Reply|Phản hồi|View \d+|Xem \d+|\d{1,2}-\d{1,2}|\d{4}-\d{1,2}-\d{1,2})$/i.test(t)) continue;
                        add(t);
                        break;
                    }
                };

                document.querySelectorAll('[data-e2e=""comment-level-1""], [data-e2e=""comment-level-2""]')
                    .forEach(pickFromItem);

                document.querySelectorAll('div[class*=""CommentItem""]').forEach(pickFromItem);

                return texts;
            }
        ")).ToList();
    }

    private static async Task<List<string>> ExtractCommentsFromJsonAsync(IPage page)
    {
        try
        {
            return (await page.EvaluateAsync<string[]>(@"
                () => {
                    const texts = new Set();
                    const add = (t) => {
                        if (typeof t === 'string' && t.trim().length > 2 && t.trim().length < 500)
                            texts.add(t.trim());
                    };
                    const walk = (obj) => {
                        if (!obj || typeof obj !== 'object') return;
                        if (Array.isArray(obj)) { obj.forEach(walk); return; }
                        if (obj.text && (obj.cid || obj.comment_id)) add(obj.text);
                        Object.values(obj).forEach(walk);
                    };
                    const el = document.querySelector('#SIGI_STATE')
                        || document.querySelector('script[id=""__UNIVERSAL_DATA_FOR_REHYDRATION__""]');
                    if (el && el.textContent) {
                        try { walk(JSON.parse(el.textContent)); } catch {}
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

    private static async Task TryExpandCommentPanelAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "[data-e2e='comment-count']",
            "[data-e2e='browse-comment-count']",
            "strong[class*='CommentCount']",
            "span:has-text('bình luận')",
            "span:has-text('comments')"
        })
        {
            try
            {
                var el = page.Locator(selector).First;
                if (await el.IsVisibleAsync())
                {
                    await el.ClickAsync();
                    await page.WaitForTimeoutAsync(1500);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task TryOpenCommentsAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "[data-e2e='comment-icon']",
            "[data-e2e='browse-comment-icon']",
            "button:has-text('Bình luận')",
            "button:has-text('Comments')"
        })
        {
            try
            {
                var btn = page.Locator(selector).First;
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                    await page.WaitForTimeoutAsync(1200);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task TryActivateCommentsTabAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "[data-e2e='browse-comment-tab']",
            "div[role='tab']:has-text('Comments')",
            "div[role='tab']:has-text('Bình luận')",
            "button:has-text('Comments')",
            "button:has-text('Bình luận')"
        })
        {
            try
            {
                var tab = page.Locator(selector).First;
                if (await tab.IsVisibleAsync())
                {
                    await tab.ClickAsync();
                    await page.WaitForTimeoutAsync(1000);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task ExpandReplyThreadsAsync(IPage page)
    {
        try
        {
            var buttons = page.Locator("button, span, p, div")
                .Filter(new LocatorFilterOptions
                {
                    HasTextRegex = new Regex(
                        @"View \d+ repl|Xem \d+ phản hồi|view \d+ replies",
                        RegexOptions.IgnoreCase)
                });

            var count = Math.Min(await buttons.CountAsync(), 10);
            for (var i = 0; i < count; i++)
            {
                try
                {
                    await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    await page.WaitForTimeoutAsync(700);
                }
                catch { }
            }
        }
        catch { }
    }

    private static async Task ScrollCommentsAsync(IPage page, int maxComments, bool aggressive = false)
    {
        var previousCount = 0;
        var noChangeStreak = 0;
        var iterations = aggressive ? 20 : 15;

        for (var i = 0; i < iterations; i++)
        {
            var currentCount = await page.Locator(
                "[data-e2e='comment-level-1'], [data-e2e='comment-level-2']").CountAsync();
            if (currentCount >= maxComments) break;

            if (currentCount == previousCount)
            {
                noChangeStreak++;
                if (noChangeStreak >= 4) break;
            }
            else noChangeStreak = 0;

            previousCount = currentCount;

            await page.EvaluateAsync(@"() => {
                const panels = [
                    document.querySelector('[class*=""CommentListContainer""]'),
                    document.querySelector('[class*=""comment-list""]'),
                    document.querySelector('[data-e2e=""comment-list""]'),
                    document.querySelector('div[class*=""DivCommentList""]')
                ].filter(Boolean);
                for (const panel of panels) panel.scrollTop += 900;
                window.scrollBy(0, 600);
            }");

            await page.Mouse.WheelAsync(0, aggressive ? 1600 : 1200);
            await page.WaitForTimeoutAsync(aggressive ? 1100 : 900);
        }
    }

    private sealed class OEmbedResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author_name")]
        public string? AuthorName { get; set; }
    }
}
