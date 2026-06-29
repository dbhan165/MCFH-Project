using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public class FacebookCommentScraper
{
    public async Task<List<string>> ScrapePostCommentsAsync(string postUrl, ScrapeOptions options, Proxy? proxy = null)
    {
        var max = Math.Max(options.MaxCommentsPerItem, 40);

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                PlaywrightScrapeHelper.SocialLaunch(options, proxy));

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
                Locale = "vi-VN"
            });

            await FacebookSessionHelper.LoadCookiesAsync(context);
            var page = await context.NewPageAsync();

            var results = await FacebookCommentExtractor.ScrapeFromPostUrlAsync(page, postUrl, max);
            Console.WriteLine($"[FB Comments] {postUrl} → {results.Count} text comments");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FB Comments] Lỗi {postUrl}: {ex.Message}");
            return new List<string>();
        }
    }
}
