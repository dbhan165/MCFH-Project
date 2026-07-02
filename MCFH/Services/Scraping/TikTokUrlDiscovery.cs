using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class TikTokStealthHelper
{
    public static BrowserTypeLaunchOptions CreateLaunchOptions(bool headless, Proxy? proxy = null)
    {
        var args = new List<string>
        {
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--disable-extensions",
            "--no-first-run",
            "--disable-default-apps"
        };

        if (headless)
            args.Add("--headless=new");

        return new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = args.ToArray(),
            Proxy = proxy
        };
    }

    public static async Task<IBrowserContext> CreateContextAsync(IBrowser browser)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
            Locale = "vi-VN",
            TimezoneId = "Asia/Ho_Chi_Minh",
            DeviceScaleFactor = 1,
            HasTouch = false,
            IsMobile = false,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept-Language"] = "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7",
                ["Sec-Ch-Ua"] = "\"Chromium\";v=\"131\", \"Google Chrome\";v=\"131\", \"Not_A Brand\";v=\"24\"",
                ["Sec-Ch-Ua-Mobile"] = "?0",
                ["Sec-Ch-Ua-Platform"] = "\"Windows\""
            }
        });

        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['vi-VN', 'vi', 'en-US', 'en'] });
            window.chrome = { runtime: {}, loadTimes: function(){}, csi: function(){} };
        ");

        await TikTokSessionHelper.LoadCookiesIfExistsAsync(context);
        return context;
    }

    public static async Task DismissCookieBannersAsync(IPage page)
    {
        foreach (var selector in new[]
        {
            "button:has-text('Allow all')",
            "button:has-text('Cho phép tất cả')",
            "button:has-text('Accept all')",
            "button:has-text('Từ chối')",
            "button:has-text('Decline optional')",
            "[data-e2e='reject-all']",
            "[data-e2e='accept-all']"
        })
        {
            try
            {
                var btn = page.Locator(selector).First;
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                    await TikTokHumanizeHelper.AfterClickAsync(page, null);
                    return;
                }
            }
            catch { }
        }
    }

    /// <summary>Đóng cookie banner, popup passkey và modal chặn tương tự trước khi cào.</summary>
    public static async Task DismissBlockingDialogsAsync(IPage page)
    {
        await DismissCookieBannersAsync(page);
        await DismissPasskeyPromptAsync(page);
    }

    private static async Task DismissPasskeyPromptAsync(IPage page)
    {
        await TikTokHumanizeHelper.DelayAsync(page, null, 400, 300, 700);

        foreach (var label in new[]
                 {
                     "Để sau", "Maybe later", "Not now", "Skip for now", "Later", "Bỏ qua"
                 })
        {
            try
            {
                var btn = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true }).First;
                await btn.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 300
                });
                await btn.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                await TikTokHumanizeHelper.AfterClickAsync(page, null);
                Console.WriteLine($"[TikTok] Đã đóng popup passkey (nút «{label}»).");
                return;
            }
            catch { }
        }

        try
        {
            var dismissed = await page.EvaluateAsync<bool>(@"
                () => {
                    const body = (document.body?.innerText || '').toLowerCase();
                    const isPasskey = body.includes('passkey')
                        || body.includes('pass key')
                        || body.includes('khóa truy cập')
                        || body.includes('đăng nhập dễ dàng');
                    if (!isPasskey) return false;

                    const laterLabels = ['để sau', 'maybe later', 'not now', 'later', 'skip', 'bỏ qua'];
                    for (const el of document.querySelectorAll('button, [role=""button""], div[tabindex=""0""]')) {
                        const t = (el.innerText || '').trim().toLowerCase();
                        if (!t) continue;
                        if (laterLabels.some(l => t === l || t.startsWith(l + ' '))) {
                            el.click();
                            return true;
                        }
                    }
                    return false;
                }
            ");

            if (dismissed)
            {
                await TikTokHumanizeHelper.AfterClickAsync(page, null);
                Console.WriteLine("[TikTok] Đã đóng popup passkey (JS fallback).");
            }
        }
        catch { }
    }
}

public static class TikTokUrlDiscovery
{
    private static readonly Regex VideoUrlRegex = new(
        @"https?://(?:www\.)?tiktok\.com/@[\w.\-]+/video/\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IEnumerable<string> BuildKeywordVariants(string keyword)
    {
        var variants = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            variants.Add(keyword.Trim());

        var firstWord = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstWord))
            variants.Add(firstWord);

        var compact = keyword.Replace(" ", "", StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(compact))
            variants.Add(compact);

        return variants.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<List<string>> DiscoverAsync(
        IPage page,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default,
        bool sessionHeadless = true,
        TikTokCaptchaTracker? captchaTracker = null)
    {
        var all = new List<string>();

        foreach (var variant in BuildKeywordVariants(keyword))
        {
            cancellationToken.ThrowIfCancellationRequested();
            onStatus?.Invoke($"TikTok: đang tìm «{variant}»...");
            Console.WriteLine($"[TikTok] Discover keyword variant: '{variant}'");

            all.AddRange(await SearchWithNetworkCaptureAsync(
                page, variant, maxVideos, options, onStatus, cancellationToken, sessionHeadless, captchaTracker));
            all = NormalizeUrls(all);

            if (all.Count < maxVideos)
            {
                onStatus?.Invoke("TikTok: thử hashtag...");
                all.AddRange(await SearchViaTagAsync(
                    page, variant, maxVideos, options, onStatus, cancellationToken, sessionHeadless, captchaTracker));
            }
            all = NormalizeUrls(all);

            if (all.Count < maxVideos)
            {
                onStatus?.Invoke("TikTok: thử tìm qua Google...");
                all.AddRange(await SearchViaGoogleAsync(
                    page, variant, maxVideos, options, onStatus, cancellationToken, sessionHeadless, captchaTracker));
            }
            all = NormalizeUrls(all);

            if (all.Count < maxVideos)
            {
                onStatus?.Invoke("TikTok: thử tìm qua Bing...");
                all.AddRange(await SearchViaBingAsync(page, variant, maxVideos, onStatus, cancellationToken));
            }
            all = NormalizeUrls(all);

            if (all.Count < maxVideos)
            {
                onStatus?.Invoke("TikTok: thử cuộn trang tìm kiếm...");
                all.AddRange(await SearchOnTikTokAsync(
                    page, variant, maxVideos, options, onStatus, cancellationToken, sessionHeadless, captchaTracker));
            }
            all = NormalizeUrls(all);

            if (all.Count > 0)
                break;
        }

        return all.Take(maxVideos).ToList();
    }

    private static async Task<List<string>> SearchWithNetworkCaptureAsync(
        IPage page,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken,
        bool sessionHeadless = true,
        TikTokCaptchaTracker? captchaTracker = null)
    {
        var capture = new TikTokNetworkCapture();
        capture.Attach(page);

        var encoded = Uri.EscapeDataString(keyword);
        var urls = new List<string>();

        foreach (var searchUrl in new[]
        {
            $"https://www.tiktok.com/search/video?q={encoded}",
            $"https://www.tiktok.com/search?q={encoded}"
        })
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                onStatus?.Invoke("TikTok: mở search và bắt API nội bộ...");
                Console.WriteLine($"[TikTok] Network capture goto: {searchUrl}");

                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 35000
                });
                await TikTokStealthHelper.DismissBlockingDialogsAsync(page);
                await TryClickVideoTabAsync(page, options);

                var waitMs = Math.Clamp(options.TikTokSearchWaitMs, 5000, 30000);
                var scrollRounds = 6;
                for (var i = 0; i < scrollRounds; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    urls.AddRange(capture.VideoUrls);
                    urls.AddRange(await ExtractFromSearchCardsAsync(page));
                    urls.AddRange(await ExtractFromEmbeddedJsonAsync(page));
                    urls = NormalizeUrls(urls);

                    onStatus?.Invoke($"TikTok: API/DOM thấy {urls.Count} video...");
                    if (urls.Count >= maxVideos)
                        break;

                    await TikTokHumanizeHelper.ScrollDownAsync(
                        page, options, 1800, waitMs / scrollRounds, cancellationToken: cancellationToken);
                }

                urls.AddRange(capture.VideoUrls);
                urls = NormalizeUrls(urls);

                if (urls.Count > 0)
                {
                    Console.WriteLine($"[TikTok] Network capture found {urls.Count} URL(s)");
                    break;
                }

                if (!await TikTokCaptchaHelper.TryContinueAsync(
                        page, options, "tìm video", sessionHeadless, captchaTracker))
                    onStatus?.Invoke("TikTok: bị CAPTCHA — thử nguồn khác...");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TikTok] Network capture error: {ex.Message}");
            }
        }

        return urls.Take(maxVideos).ToList();
    }

    private static async Task<List<string>> SearchViaTagAsync(
        IPage page,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken,
        bool sessionHeadless = true,
        TikTokCaptchaTracker? captchaTracker = null)
    {
        var capture = new TikTokNetworkCapture();
        capture.Attach(page);
        var urls = new List<string>();

        foreach (var tag in BuildHashtagVariants(keyword))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var tagUrl = $"https://www.tiktok.com/tag/{Uri.EscapeDataString(tag)}";
                onStatus?.Invoke($"TikTok: mở #{tag}...");
                Console.WriteLine($"[TikTok] Tag page: {tagUrl}");

                await page.GotoAsync(tagUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });
                await TikTokStealthHelper.DismissBlockingDialogsAsync(page);
                await TikTokCaptchaHelper.TryContinueAsync(
                    page, options, "hashtag", sessionHeadless, captchaTracker);

                for (var i = 0; i < 5; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    urls.AddRange(capture.VideoUrls);
                    urls.AddRange(await ExtractFromPageAsync(page));
                    urls.AddRange(await ExtractFromEmbeddedJsonAsync(page));
                    urls = NormalizeUrls(urls);
                    if (urls.Count >= maxVideos) break;
                    await TikTokHumanizeHelper.ScrollDownAsync(
                        page, options, 1600, 1200, cancellationToken: cancellationToken);
                }

                if (urls.Count > 0) break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TikTok] Tag search error: {ex.Message}");
            }
        }

        return urls.Take(maxVideos).ToList();
    }

    public static IEnumerable<string> BuildHashtagVariants(string keyword)
    {
        var variants = new List<string>();
        var compact = new string(keyword.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(compact))
            variants.Add(compact);

        var underscored = string.Join("_", keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(underscored))
            variants.Add(underscored);

        var first = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(first))
            variants.Add(first);

        return variants.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<List<string>> ExtractFromSearchCardsAsync(IPage page)
    {
        try
        {
            return (await page.EvaluateAsync<string[]>(@"
                () => {
                    const urls = new Set();
                    const add = (href) => {
                        if (!href) return;
                        let u = href;
                        if (u.startsWith('/')) u = 'https://www.tiktok.com' + u;
                        if (u.includes('/video/')) urls.add(u.split('?')[0].split('#')[0]);
                    };

                    document.querySelectorAll('div[id^=""column-item-video-container""] a[href]').forEach(a => add(a.href));
                    document.querySelectorAll('div[data-e2e=""search_top-item""] a[href]').forEach(a => add(a.href));
                    document.querySelectorAll('div[data-e2e=""search-common-link""]').forEach(a => add(a.href || a.getAttribute('href')));
                    document.querySelectorAll('a[href*=""/video/""]').forEach(a => add(a.href || a.getAttribute('href')));

                    return Array.from(urls);
                }
            ")).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static async Task<List<string>> ExtractFromEmbeddedJsonAsync(IPage page)
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

            return string.IsNullOrWhiteSpace(jsonText)
                ? new List<string>()
                : TikTokApiParser.ExtractVideoUrlsFromJson(jsonText).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static async Task<List<string>> SearchOnTikTokAsync(
        IPage page,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken,
        bool sessionHeadless = true,
        TikTokCaptchaTracker? captchaTracker = null)
    {
        var encoded = Uri.EscapeDataString(keyword);
        var urls = new List<string>();

        foreach (var searchUrl in new[]
        {
            $"https://www.tiktok.com/search/video?q={encoded}",
            $"https://www.tiktok.com/search?q={encoded}"
        })
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                onStatus?.Invoke("TikTok: mở trang tìm kiếm...");
                Console.WriteLine($"[TikTok] Goto: {searchUrl}");
                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });
                await TikTokStealthHelper.DismissBlockingDialogsAsync(page);
                if (!await TikTokCaptchaHelper.TryContinueAsync(
                        page, options, "tìm video", sessionHeadless, captchaTracker))
                {
                    onStatus?.Invoke("TikTok: bị CAPTCHA — thử cách khác...");
                    Console.WriteLine("[TikTok] CAPTCHA khi tìm kiếm — thử URL search khác hoặc fallback Google.");
                    continue;
                }
                await TikTokHumanizeHelper.AfterNavigationAsync(page, options, cancellationToken);

                await TryClickVideoTabAsync(page, options);

                for (var i = 0; i < 8; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    urls.AddRange(await ExtractFromPageAsync(page));
                    urls = NormalizeUrls(urls);
                    onStatus?.Invoke($"TikTok: đã thấy {urls.Count} video...");
                    Console.WriteLine($"[TikTok] Search scroll {i + 1}: {urls.Count} URLs");

                    if (urls.Count >= maxVideos) break;
                    await TikTokHumanizeHelper.ScrollDownAsync(
                        page, options, 2000, 1000, cancellationToken: cancellationToken);
                }

                if (urls.Count > 0) break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TikTok] Search error: {ex.Message}");
            }
        }

        return urls.Take(maxVideos).ToList();
    }

    private static async Task<List<string>> SearchViaGoogleAsync(
        IPage page,
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken,
        bool sessionHeadless = true,
        TikTokCaptchaTracker? captchaTracker = null)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = Uri.EscapeDataString($"site:tiktok.com/video {keyword}");
            var url = $"https://www.google.com/search?q={query}&num=10";
            onStatus?.Invoke("TikTok: tìm qua Google...");
            Console.WriteLine($"[TikTok] Google fallback: {keyword}");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });
            await TikTokStealthHelper.DismissBlockingDialogsAsync(page);
            if (!await TikTokCaptchaHelper.TryContinueAsync(
                    page, options, "Google", sessionHeadless, captchaTracker))
            {
                onStatus?.Invoke("TikTok: Google bị CAPTCHA/chặn.");
                return new List<string>();
            }
            await TikTokHumanizeHelper.AfterNavigationAsync(page, options, cancellationToken);

            var found = await ExtractFromPageAsync(page);
            return found.Take(maxVideos).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok] Google search error: {ex.Message}");
            return new List<string>();
        }
    }

    private static async Task<List<string>> SearchViaBingAsync(
        IPage page,
        string keyword,
        int maxVideos,
        Action<string>? onStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = Uri.EscapeDataString($"site:tiktok.com/video {keyword}");
            var url = $"https://www.bing.com/search?q={query}&count=10";
            onStatus?.Invoke("TikTok: tìm qua Bing...");
            Console.WriteLine($"[TikTok] Bing fallback: {keyword}");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });
            await TikTokHumanizeHelper.AfterNavigationAsync(page, null, cancellationToken);

            var found = await ExtractFromPageAsync(page);
            return found.Take(maxVideos).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok] Bing search error: {ex.Message}");
            return new List<string>();
        }
    }

    private static async Task TryClickVideoTabAsync(IPage page, ScrapeOptions? options = null)
    {
        foreach (var selector in new[]
        {
            "[data-e2e='search-video-tab']",
            "div[role='tab']:has-text('Video')",
            "button:has-text('Video')",
            "span:has-text('Videos')"
        })
        {
            try
            {
                var tab = page.Locator(selector).First;
                if (await tab.IsVisibleAsync())
                {
                    await tab.ClickAsync();
                    await TikTokHumanizeHelper.AfterClickAsync(page, options);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task<List<string>> ExtractFromPageAsync(IPage page)
    {
        var fromDom = await page.EvaluateAsync<string[]>(@"
            () => {
                const urls = new Set();
                const add = (href) => {
                    if (!href) return;
                    let u = href;
                    if (u.startsWith('/')) u = 'https://www.tiktok.com' + u;
                    if (u.includes('/video/') && !u.includes('/search')) {
                        urls.add(u.split('?')[0].split('#')[0]);
                    }
                };
                document.querySelectorAll('a[href]').forEach(a => add(a.href || a.getAttribute('href')));
                return Array.from(urls);
            }
        ");

        var html = await page.ContentAsync();
        var fromHtml = VideoUrlRegex.Matches(html).Select(m => m.Value.Split('?')[0]).ToList();

        return NormalizeUrls(fromDom.Concat(fromHtml).ToList());
    }

    private static List<string> NormalizeUrls(IEnumerable<string> urls) =>
        urls
            .Select(u => u.Trim())
            .Select(u => u.Replace("https://m.tiktok.com", "https://www.tiktok.com", StringComparison.OrdinalIgnoreCase))
            .Where(u => VideoUrlRegex.IsMatch(u))
            .Select(u => u.Split('?')[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static void ParseAuthorFromUrl(string videoUrl, ScrapeResult result)
    {
        var match = Regex.Match(videoUrl, @"tiktok\.com/@([^/]+)/video", RegexOptions.IgnoreCase);
        if (match.Success && string.IsNullOrWhiteSpace(result.Author))
            result.Author = "@" + match.Groups[1].Value;
    }
}
