using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class PlaywrightScrapeHelper
{
  public static BrowserTypeLaunchOptions YouTubeLaunch(ScrapeOptions options, Proxy? proxy = null) =>
      CreateHeadlessLaunch(options.YouTubeHeadless, proxy);

  public static BrowserTypeLaunchOptions SocialLaunch(ScrapeOptions scrapeOptions, Proxy? proxy = null)
  {
    Console.WriteLine($"[Playwright] SocialHeadless={scrapeOptions.SocialHeadless}");
    var launchOpts = CreateHeadlessLaunch(scrapeOptions.SocialHeadless, proxy);
    Console.WriteLine($"[Playwright] launchOpts.Headless={launchOpts.Headless}");

    var exe = FindBrowserExe(launchOpts.Headless == true);
    Console.WriteLine($"[Playwright] FindBrowserExe (headless={launchOpts.Headless == true}) result: {exe ?? "NULL"}");
    if (exe != null)
    {
      Console.WriteLine($"[Playwright] SocialLaunch dùng: {exe}");
      launchOpts.ExecutablePath = exe;
    }
    else
    {
      Console.WriteLine("[Playwright] KHÔNG tìm thấy browser - để Playwright tự chọn (có thể fail nếu cache ngoài dự án).");
    }
    return launchOpts;
  }

  private static string? FindBrowserExe(bool headless)
  {
    var candidates = new List<string>();
    var baseDir = AppContext.BaseDirectory;
    var dir = new DirectoryInfo(baseDir);
    while (dir != null)
    {
      candidates.Add(Path.Combine(dir.FullName, ".playwright"));
      dir = dir.Parent;
    }
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), ".playwright"));
    candidates.Add(@"D:\1_SEP490\main_project\MCFH-Project\MCFH\.playwright");

    var folder = headless ? "chromium_headless_shell-*" : "chromium-*";
    var subDirs = headless
      ? new[] { "chrome-headless-shell-win64", "chrome-headless-shell-win" }
      : new[] { "chrome-win64", "chrome-win" };
    var exeName = headless ? "chrome-headless-shell.exe" : "chrome.exe";

    foreach (var pwDir in candidates)
    {
      if (!Directory.Exists(pwDir)) continue;
      var matchingDirs = Directory.GetDirectories(pwDir, folder);
      foreach (var cdir in matchingDirs)
      {
        foreach (var sub in subDirs)
        {
          var exe = Path.Combine(cdir, sub, exeName);
          if (File.Exists(exe)) return exe;
        }
      }
    }
    return null;
  }

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
    else
      args.Add("--start-maximized");

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
