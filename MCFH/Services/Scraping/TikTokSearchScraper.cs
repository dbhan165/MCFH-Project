using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public class TikTokSearchScraper
{
    public TikTokSearchScraper() { }

    public async Task<List<string>> SearchAsync(string keyword, int maxVideos = 10)
    {
        var results = new List<string>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 500
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        var page = await context.NewPageAsync();
        var encodedKeyword = Uri.EscapeDataString(keyword);
        var searchUrl = $"https://www.tiktok.com/search?q={encodedKeyword}";

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Console.WriteLine($"[TikTok] Attempt {attempt}: Navigating to {searchUrl}");
            await page.GotoAsync(searchUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });
            await page.WaitForTimeoutAsync(3000);

            var errorEl = await page.QuerySelectorAsync("text=Something went wrong");
            if (errorEl != null)
            {
                Console.WriteLine($"[TikTok] Attempt {attempt}: Error page detected, retrying...");
                await page.WaitForTimeoutAsync(2000);
                continue;
            }

            Console.WriteLine($"[TikTok] Attempt {attempt}: Page OK, scrolling...");
            await page.Mouse.MoveAsync(640, 400);

            for (int i = 0; i < 10; i++)
            {
                await page.Mouse.WheelAsync(0, 1500);
                await page.WaitForTimeoutAsync(1500);

                var current = await ExtractVideoUrls(page);
                Console.WriteLine($"[TikTok] Scroll {i + 1}: found {current.Count} URLs so far");

                if (current.Count >= maxVideos)
                    break;
            }

            results = await ExtractVideoUrls(page);
            break;
        }

        Console.WriteLine($"[TikTok] Final result: {results.Count} URLs");
        return results.Take(maxVideos).ToList();
    }

    private async Task<List<string>> ExtractVideoUrls(IPage page)
    {
        var hrefs = await page.EvalOnSelectorAllAsync<string[]>(
            "a[href*='/video/']",
            "elements => elements.map(e => e.href)"
        );

        return hrefs
            .Where(h => h.Contains("/video/") && !h.Contains("/search"))
            .Distinct()
            .ToList();
    }
}