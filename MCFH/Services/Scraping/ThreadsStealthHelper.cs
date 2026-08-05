using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class ThreadsStealthHelper
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
            "--disable-default-apps",
            "--disable-popups-blocking"
        };

        if (headless)
            args.Add("--headless=new");
        else
            args.Add("--start-maximized");

        var launchOpts = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = args.ToArray(),
            Proxy = proxy
        };

        // In sandboxed/CI environments, Playwright's default cache may be redirected to a temp dir
        // that does not contain the browser. Resolve to our local .playwright folder explicitly.
        var exe = ResolveBrowserExe(headless);
        if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
        {
            Console.WriteLine($"[Threads] Browser ExecutablePath: {exe}");
            launchOpts.ExecutablePath = exe;
        }
        return launchOpts;
    }

    private static string? ResolveBrowserExe(bool headless)
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

    public static async Task<IBrowserContext> CreateContextAsync(
        IBrowser browser,
        bool headless,
        Action<string>? onStatus = null)
    {
        onStatus?.Invoke("[Threads] Creating browser context...");

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

        onStatus?.Invoke("[Threads] Browser context ready.");
        return context;
    }

    /// <summary>Đóng cookie banner và các popup chặn trên Threads.</summary>
    public static async Task DismissBlockingDialogsAsync(IPage page, Action<string>? onStatus = null)
    {
        await DismissCookieBannersAsync(page, onStatus);
        await DismissLoginPromptAsync(page, onStatus);
    }

    private static async Task DismissCookieBannersAsync(IPage page, Action<string>? onStatus)
    {
        foreach (var selector in new[]
        {
            "button[aria-label*='Accept' i]",
            "button[aria-label*='Allow' i]",
            "button:has-text('Accept all')",
            "button:has-text('Allow all')",
            "button:has-text('Cho phép')",
            "[data-testid='cookie-policy-dialog'] button",
            "div[data-cookiebanner]:not([aria-hidden]) button"
        })
        {
            try
            {
                var btn = page.Locator(selector).First;
                if (await btn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await btn.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    onStatus?.Invoke($"[Threads] Dismissed cookie banner via: {selector}");
                    await Task.Delay(300);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task DismissLoginPromptAsync(IPage page, Action<string>? onStatus)
    {
        var dismissLabels = new[]
        {
            "Not now", "Skip", "Bỏ qua", "Later", "Maybe later",
            "Để sau", "Cancel", "Hủy"
        };

        foreach (var label in dismissLabels)
        {
            try
            {
                var btn = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = false }).First;
                if (await btn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 500 }))
                {
                    await btn.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    onStatus?.Invoke($"[Threads] Dismissed login prompt: '{label}'");
                    await Task.Delay(300);
                    return;
                }
            }
            catch { }
        }
    }

    /// <summary>Chờ ngẫu nhiên mô phỏng hành vi người dùng.</summary>
    public static async Task DelayAsync(Random rng, int minMs, int maxMs)
    {
        var delay = rng.Next(minMs, maxMs + 1);
        await Task.Delay(delay);
    }
}
