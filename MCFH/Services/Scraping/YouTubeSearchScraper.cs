using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public class YouTubeSearchScraper
{
    public async Task<List<string>> SearchAsync(string keyword, int maxVideos, ScrapeOptions options, IBrowser? sharedBrowser = null)
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
                await page.GotoAsync(
                    $"https://www.youtube.com/results?search_query={encodedKeyword}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

                var scrollsNeeded = Math.Max(1, (int)Math.Ceiling(maxVideos / 6.0));
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
}
