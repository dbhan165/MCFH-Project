using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public class YouTubeSearchScraper
{
    public async Task<List<string>> SearchAsync(
        string keyword,
        int maxVideos,
        ScrapeOptions options,
        IBrowser? sharedBrowser = null,
        int? postedSinceDays = null)
    {
        var ownsBrowser = sharedBrowser == null;
        IPlaywright? playwright = null;
        IBrowser? browser = sharedBrowser;

        try
        {
            if (browser == null)
            {
                playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync(PlaywrightScrapeHelper.YouTubeLaunch(options));
            }

            var page = await browser.NewPageAsync();
            try
            {
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var uploadFilter = GetYouTubeUploadDateFilter(postedSinceDays);
                var searchUrl = string.IsNullOrEmpty(uploadFilter)
                    ? $"https://www.youtube.com/results?search_query={encodedKeyword}"
                    : $"https://www.youtube.com/results?search_query={encodedKeyword}&sp={uploadFilter}";

                await NavigateSearchWithRetryAsync(page, searchUrl, options);

                var scrollsNeeded = Math.Max(2, (int)Math.Ceiling(maxVideos / 4.0));
                for (int i = 0; i < scrollsNeeded; i++)
                {
                    await page.Keyboard.PressAsync("End");
                    await Task.Delay(700);
                }

                var linkElements = await page.QuerySelectorAllAsync(
                    "a#video-title, ytd-video-renderer a#video-title, a.yt-simple-endpoint.style-scope.ytd-video-renderer");
                var urls = new List<string>();
                foreach (var el in linkElements)
                {
                    var href = await el.GetAttributeAsync("href");
                    if (href is not null && href.StartsWith("/watch"))
                    {
                        urls.Add("https://www.youtube.com" + href);
                        if (urls.Count >= maxVideos) break;
                    }
                }

                return urls;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        finally
        {
            if (ownsBrowser && browser != null)
                await browser.DisposeAsync();
            playwright?.Dispose();
        }
    }

    private static async Task NavigateSearchWithRetryAsync(IPage page, string searchUrl, ScrapeOptions options)
    {
        var timeout = Math.Clamp(options.YouTubeNavigationTimeoutMs, 30_000, 180_000);
        var attempts = new[]
        {
            (Wait: WaitUntilState.DOMContentLoaded, Timeout: timeout),
            (Wait: WaitUntilState.Load, Timeout: Math.Min(timeout + 30_000, 180_000))
        };

        Exception? last = null;
        foreach (var (waitUntil, attemptTimeout) in attempts)
        {
            try
            {
                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = waitUntil,
                    Timeout = attemptTimeout
                });
                return;
            }
            catch (Exception ex) when (IsNavigationTimeout(ex))
            {
                last = ex;
                Console.WriteLine($"[YouTube] Search timeout ({waitUntil}, {attemptTimeout}ms) — thử lại...");
            }
        }

        throw last ?? new TimeoutException($"YouTube search timeout sau {attempts.Length} lần.");
    }

    private static bool IsNavigationTimeout(Exception ex) =>
        ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);

    /// <summary>YouTube search upload-date filters (sp parameter).</summary>
    private static string? GetYouTubeUploadDateFilter(int? postedSinceDays) =>
        postedSinceDays switch
        {
            null or <= 0 => null,
            <= 1 => "EgQIARAB",
            <= 7 => "EgIIAw%3D%3D",
            <= 30 => "EgQIAhAB",
            <= 365 => "EgQIAxAB",
            _ => null
        };
}
