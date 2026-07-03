using MCFH.Configuration;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public sealed class NewsArticleResult
{
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class NewsSearchScraper
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s""'<>]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string BuildSiteQuery(string keyword, IReadOnlyList<string> sites)
    {
        var siteClause = string.Join(" OR ", sites.Select(s => $"site:{s.Trim()}"));
        return $"{siteClause} {keyword.Trim()}";
    }

    public static async Task<List<string>> DiscoverArticleUrlsAsync(
        string keyword,
        int maxArticles,
        ScrapeOptions options,
        SerpApiNewsDiscovery serpApi,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default,
        Proxy? proxy = null)
    {
        var sites = NewsUrlFilter.NormalizeSites(options.NewsSearchSites);
        var query = BuildSiteQuery(keyword, sites);
        var provider = (options.NewsDiscoveryProvider ?? "auto").Trim().ToLowerInvariant();

        var trySerpApi = provider switch
        {
            "playwright" => false,
            "serpapi" => true,
            _ => serpApi.IsConfigured
        };

        if (trySerpApi)
        {
            onStatus?.Invoke("Tin tức: tìm qua SerpApi...");
            var serpResult = await serpApi.DiscoverArticleUrlsAsync(query, sites, maxArticles, cancellationToken);

            if (serpResult.Urls.Count > 0)
                return serpResult.Urls.Take(maxArticles).ToList();

            if (!serpResult.ShouldFallback)
                return [];

            var reason = string.IsNullOrWhiteSpace(serpResult.Message)
                ? "SerpApi không có kết quả"
                : serpResult.Message;
            onStatus?.Invoke($"Tin tức: {reason} — thử Playwright...");
            Console.WriteLine($"[News] SerpApi fallback → Playwright: {reason}");
        }
        else if (provider is "auto" or "serpapi")
        {
            Console.WriteLine("[News] SerpApi chưa cấu hình — dùng Playwright discovery.");
        }

        return await DiscoverArticleUrlsViaPlaywrightAsync(
            query, sites, maxArticles, options, onStatus, cancellationToken, proxy);
    }

    private static async Task<List<string>> DiscoverArticleUrlsViaPlaywrightAsync(
        string query,
        IReadOnlyList<string> sites,
        int maxArticles,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken,
        Proxy? proxy)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            PlaywrightScrapeHelper.CreateHeadlessLaunch(options.NewsHeadless, proxy));

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

        var page = await context.NewPageAsync();
        try
        {
            var urls = await SearchGoogleAsync(page, query, sites, maxArticles, options, onStatus, cancellationToken);
            if (urls.Count == 0)
            {
                onStatus?.Invoke("Tin tức: Google không có kết quả — thử Bing...");
                urls = await SearchBingAsync(page, query, sites, maxArticles, options, onStatus, cancellationToken);
            }

            return urls.Take(maxArticles).ToList();
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    public static async Task<NewsArticleResult> ScrapeArticleAsync(
        string url,
        ScrapeOptions options,
        IBrowserContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new NewsArticleResult();
        var cleanUrl = url.Split('#')[0].Split('?')[0].Trim();

        var page = await context.NewPageAsync();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"[News] Opening article: {cleanUrl}");

            await page.GotoAsync(cleanUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await TikTokHumanizeHelper.AfterNavigationAsync(page, options, cancellationToken);

            result.Title = await page.EvaluateAsync<string?>(@"() => {
                return document.querySelector('meta[property=""og:title""]')?.getAttribute('content')
                    || document.querySelector('meta[name=""twitter:title""]')?.getAttribute('content')
                    || document.querySelector('h1')?.textContent?.trim()
                    || '';
            }");

            var description = await page.EvaluateAsync<string?>(@"() => {
                return document.querySelector('meta[property=""og:description""]')?.getAttribute('content')
                    || document.querySelector('meta[name=""description""]')?.getAttribute('content')
                    || '';
            }");

            var body = await page.EvaluateAsync<string?>(@"() => {
                const selectors = [
                    'article',
                    '[class*=""article""][class*=""content""]',
                    '[class*=""Article""][class*=""body""]',
                    '[class*=""detail-content""]',
                    '[class*=""fck_detail""]',
                    '.main-content',
                    '#maincontent'
                ];
                for (const sel of selectors) {
                    const el = document.querySelector(sel);
                    if (el && el.innerText && el.innerText.trim().length > 80)
                        return el.innerText.trim();
                }
                const paras = Array.from(document.querySelectorAll('p'))
                    .map(p => (p.innerText || '').trim())
                    .filter(t => t.length > 40);
                if (paras.length > 0) return paras.slice(0, 8).join('\n');
                return '';
            }");

            result.Content = !string.IsNullOrWhiteSpace(body)
                ? body!.Trim()
                : description?.Trim();

            result.Author = await page.EvaluateAsync<string?>(@"() => {
                return document.querySelector('meta[name=""author""]')?.getAttribute('content')
                    || document.querySelector('[rel=""author""]')?.textContent?.trim()
                    || document.querySelector('[class*=""author""]')?.textContent?.trim()
                    || '';
            }");

            var dateRaw = await page.EvaluateAsync<string?>(@"() => {
                const metas = [
                    document.querySelector('meta[property=""article:published_time""]')?.getAttribute('content'),
                    document.querySelector('meta[property=""og:article:published_time""]')?.getAttribute('content'),
                    document.querySelector('meta[itemprop=""datePublished""]')?.getAttribute('content'),
                    document.querySelector('time')?.getAttribute('datetime'),
                    document.querySelector('time')?.textContent
                ];
                return metas.find(Boolean) || null;
            }");

            if (PostedAtParser.TryParseAny(dateRaw, out var postedAt))
                result.PostedAt = postedAt;

            if (string.IsNullOrWhiteSpace(result.Title) && string.IsNullOrWhiteSpace(result.Content))
            {
                result.Success = false;
                result.ErrorMessage = "Không đọc được tiêu đề/nội dung bài báo.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(result.Title))
                result.Title = result.Content!.Length > 80
                    ? result.Content[..80] + "..."
                    : result.Content;

            result.Success = true;
            Console.WriteLine($"[News] OK title='{Truncate(result.Title, 50)}'");
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Console.WriteLine($"[News] Error {cleanUrl}: {ex.Message}");
            return result;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static async Task<List<string>> SearchGoogleAsync(
        IPage page,
        string query,
        IReadOnlyList<string> sites,
        int maxArticles,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}&num={Math.Max(10, maxArticles)}";
            onStatus?.Invoke("Tin tức: tìm qua Google (Playwright)...");
            Console.WriteLine($"[News] Google: {query}");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 35_000
            });
            await TikTokHumanizeHelper.AfterNavigationAsync(page, options, cancellationToken);

            var urls = await ExtractNewsUrlsFromSerpAsync(page, sites, maxArticles);
            Console.WriteLine($"[News] Google found {urls.Count} URL(s)");
            return urls;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[News] Google search error: {ex.Message}");
            return [];
        }
    }

    private static async Task<List<string>> SearchBingAsync(
        IPage page,
        string query,
        IReadOnlyList<string> sites,
        int maxArticles,
        ScrapeOptions options,
        Action<string>? onStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&count={Math.Max(10, maxArticles)}";
            onStatus?.Invoke("Tin tức: tìm qua Bing (Playwright)...");
            Console.WriteLine($"[News] Bing: {query}");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 35_000
            });
            await TikTokHumanizeHelper.AfterNavigationAsync(page, options, cancellationToken);

            var urls = await ExtractNewsUrlsFromSerpAsync(page, sites, maxArticles);
            Console.WriteLine($"[News] Bing found {urls.Count} URL(s)");
            return urls;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[News] Bing search error: {ex.Message}");
            return [];
        }
    }

    private static async Task<List<string>> ExtractNewsUrlsFromSerpAsync(
        IPage page, IReadOnlyList<string> sites, int maxArticles)
    {
        var hrefs = await page.EvaluateAsync<string[]>(@"() => {
            return Array.from(document.querySelectorAll('a[href]'))
                .map(a => a.href)
                .filter(Boolean);
        }");

        var collected = new List<string>(hrefs);
        var html = await page.ContentAsync();
        foreach (Match m in UrlRegex.Matches(html))
            collected.Add(m.Value);

        return NewsUrlFilter.FilterUrls(collected, sites, maxArticles);
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";
}
