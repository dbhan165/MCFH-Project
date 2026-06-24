using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public class YouTubeSearchScraper
{
    public async Task<List<string>> SearchAsync(string keyword, int maxVideos = 10)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false
        });
        var page = await browser.NewPageAsync();
        var encodedKeyword = Uri.EscapeDataString(keyword);
        await page.GotoAsync(
            $"https://www.youtube.com/results?search_query={encodedKeyword}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var scrollsNeeded = (int)Math.Ceiling(maxVideos / 5.0);
        for (int i = 0; i < scrollsNeeded; i++)
        {
            await page.Keyboard.PressAsync("End");
            await Task.Delay(1200);
        }

        var linkElements = await page.QuerySelectorAllAsync("a#video-title");
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
}