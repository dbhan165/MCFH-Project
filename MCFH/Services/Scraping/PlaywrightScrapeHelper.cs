using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class PlaywrightScrapeHelper
{
  public static BrowserTypeLaunchOptions YouTubeLaunch(ScrapeOptions options, Proxy? proxy = null) =>
      CreateHeadlessLaunch(options.YouTubeHeadless, proxy);

  public static BrowserTypeLaunchOptions SocialLaunch(ScrapeOptions options, Proxy? proxy = null) =>
      CreateHeadlessLaunch(options.SocialHeadless, proxy);

  public static BrowserTypeLaunchOptions CreateHeadlessLaunch(bool headless, Proxy? proxy = null)
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
      Args = args.ToArray(),
      Proxy = proxy
    };
  }

  public static async Task BlockHeavyAssetsAsync(IPage page)
  {
    await page.RouteAsync("**/*.{mp4,webm}", route => route.AbortAsync());
  }
}
