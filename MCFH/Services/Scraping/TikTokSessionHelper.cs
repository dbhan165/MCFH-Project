using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public static class TikTokSessionHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly string CookiePath =
        Path.Combine(AppContext.BaseDirectory, "tiktok_cookie.json");

    public static bool CookieFileExists() => File.Exists(CookiePath);

    public static async Task LoadCookiesIfExistsAsync(IBrowserContext context)
    {
        if (!CookieFileExists())
        {
            Console.WriteLine("[TikTok Session] No cookie file — continuing without login.");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(CookiePath);
            var entries = JsonSerializer.Deserialize<List<CookieEditorEntry>>(json);
            if (entries == null || entries.Count == 0) return;

            var cookies = entries.Select(ToPlaywrightCookie).ToList();

            await context.AddCookiesAsync(cookies);
            Console.WriteLine($"[TikTok Session] Loaded {cookies.Count} cookies.");
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
            var cookies = await context.CookiesAsync();
            var tiktokCookies = cookies
                .Where(c => c.Domain.Contains("tiktok", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (tiktokCookies.Count == 0)
                return;

            var entries = tiktokCookies.Select(ToCookieEditorEntry).ToList();
            await File.WriteAllTextAsync(CookiePath, JsonSerializer.Serialize(entries, JsonOptions));
            Console.WriteLine($"[TikTok Session] Saved {entries.Count} cookies → {CookiePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TikTok Session] Cookie save failed: {ex.Message}");
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
