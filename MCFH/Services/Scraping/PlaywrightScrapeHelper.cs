using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class PlaywrightScrapeHelper
{
  public static BrowserTypeLaunchOptions YouTubeLaunch(ScrapeOptions options) =>
      CreateHeadlessLaunch(options.YouTubeHeadless);

  public static BrowserTypeLaunchOptions SocialLaunch(ScrapeOptions options) =>
      CreateHeadlessLaunch(options.SocialHeadless);

  public static BrowserTypeLaunchOptions CreateHeadlessLaunch(bool headless)
  {
    var args = new List<string>
    {
      "--no-sandbox",
      "--disable-dev-shm-usage",
      "--disable-blink-features=AutomationControlled",
      "--disable-infobars",
      "--no-first-run"
    };

    if (headless)
      args.Add("--headless=new");

    return new BrowserTypeLaunchOptions
    {
      Headless = headless,
      Args = args.ToArray()
    };
  }

  public static async Task BlockHeavyAssetsAsync(IPage page)
  {
    await page.RouteAsync("**/*.{mp4,webm}", route => route.AbortAsync());
  }
}
