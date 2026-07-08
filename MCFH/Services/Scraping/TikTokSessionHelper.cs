using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Models.Scraping;
using MCFH.Services;

namespace MCFH.Services.Scraping;

public static class TikTokSessionHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string CookiePath => ScrapeCookiePaths.TikTokCookiePath;

    public static bool CookieFileExists(string? cookieFilePath = null) =>
        File.Exists(cookieFilePath ?? CookiePath);

    public static async Task LoadCookiesIfExistsAsync(IBrowserContext context)
    {
        var cookiePath = await ResolveCookiePathAsync();
        if (!File.Exists(cookiePath))
        {
            Console.WriteLine("[TikTok Session] No cookie file — continuing without login.");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(cookiePath);
            var entries = JsonSerializer.Deserialize<List<CookieEditorEntry>>(json);
            if (entries == null || entries.Count == 0) return;

            var cookies = entries.Select(ToPlaywrightCookie).ToList();

            await context.AddCookiesAsync(cookies);
            Console.WriteLine($"[TikTok Session] Loaded {cookies.Count} cookies từ {cookiePath}.");
            await TryTouchLastUsedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok Session] Cookie load failed: {ex.Message}");
        }
    }

    public static async Task SaveCookiesAsync(IBrowserContext context)
    {
        try
        {
            var cookiePath = await ResolveCookiePathAsync();
            var cookies = await context.CookiesAsync();
            var tiktokCookies = cookies
                .Where(c => c.Domain.Contains("tiktok", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (tiktokCookies.Count == 0)
                return;

            if (File.Exists(cookiePath))
            {
                var backupPath = await ResolveBackupPathAsync(cookiePath);
                File.Copy(cookiePath, backupPath, overwrite: true);
                Console.WriteLine($"[TikTok Session] Backup cookie → {backupPath}");
            }

            var entries = tiktokCookies.Select(ToCookieEditorEntry).ToList();
            await File.WriteAllTextAsync(cookiePath, JsonSerializer.Serialize(entries, JsonOptions));
            Console.WriteLine($"[TikTok Session] Saved {entries.Count} cookies → {cookiePath}");
            await TryTouchLastUsedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok Session] Cookie save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Chỉ lưu khi phiên scrape thành công — tránh session CAPTCHA ghi đè cookie tốt trên server.
    /// </summary>
    public static async Task TrySaveAfterSuccessfulSessionAsync(
        IBrowserContext? context,
        TikTokCaptchaTracker captchaTracker,
        int newVideosSaved)
    {
        if (context == null)
            return;

        if (captchaTracker.Encountered)
        {
            Console.WriteLine("[TikTok Session] Skip save — phiên gặp CAPTCHA, giữ cookie hiện tại.");
            return;
        }

        if (newVideosSaved <= 0)
        {
            Console.WriteLine("[TikTok Session] Skip save — không lưu được video mới trong phiên này.");
            return;
        }

        await SaveCookiesAsync(context);
    }

    private static async Task<string> ResolveCookiePathAsync()
    {
        try
        {
            return await PlatformCookieRuntime.Provider.ResolveFullPathAsync("tiktok")
                   ?? CookiePath;
        }
        catch (InvalidOperationException)
        {
            return CookiePath;
        }
    }

    private static async Task<string> ResolveBackupPathAsync(string cookiePath)
    {
        try
        {
            var provider = PlatformCookieRuntime.Provider;
            var relative = Path.GetRelativePath(provider.ContentRoot, cookiePath).Replace('\\', '/');
            var backupRelative = provider.GetBackupRelativePath("tiktok", relative);
            return provider.ToFullPath(backupRelative);
        }
        catch (InvalidOperationException)
        {
            return ScrapeCookiePaths.TikTokCookieBackupPath;
        }
    }

    private static async Task TryTouchLastUsedAsync()
    {
        try
        {
            await PlatformCookieRuntime.Provider.TouchLastUsedAsync("tiktok");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static Microsoft.Playwright.Cookie ToPlaywrightCookie(CookieEditorEntry e) => new()
    {
        Name = e.Name,
        Value = e.Value,
        Domain = e.Domain,
        Path = e.Path,
        Expires = (float)(e.ExpirationDate ?? -1),
        HttpOnly = e.HttpOnly,
        Secure = e.Secure,
        SameSite = e.SameSite?.ToLower() switch
        {
            "lax" => SameSiteAttribute.Lax,
            "strict" => SameSiteAttribute.Strict,
            "no_restriction" => SameSiteAttribute.None,
            _ => SameSiteAttribute.None
        }
    };

    private static CookieEditorEntry ToCookieEditorEntry(BrowserContextCookiesResult c) => new()
    {
        Name = c.Name,
        Value = c.Value,
        Domain = c.Domain ?? ".tiktok.com",
        Path = c.Path ?? "/",
        HttpOnly = c.HttpOnly,
        Secure = c.Secure,
        SameSite = c.SameSite switch
        {
            SameSiteAttribute.Lax => "lax",
            SameSiteAttribute.Strict => "strict",
            SameSiteAttribute.None => "no_restriction",
            _ => null
        },
        ExpirationDate = c.Expires > 0 ? c.Expires : null
    };
}
