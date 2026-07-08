using Microsoft.Playwright;
using MCFH.Models.Scraping;
using MCFH.Services;
using System.Text.Json;

namespace MCFH.Services.Scraping;

public static class FacebookSessionHelper
{
    public static string CookiePath => ScrapeCookiePaths.FacebookCookiePath;

    public static bool CookieFileExists(string? cookieFilePath = null) =>
        File.Exists(cookieFilePath ?? CookiePath);

    public static async Task LoadCookiesAsync(IBrowserContext context)
    {
        var cookiePath = await ResolveCookiePathAsync();
        if (!File.Exists(cookiePath))
            throw new FileNotFoundException($"Cookie file không tồn tại tại: {cookiePath}");

        var json = await File.ReadAllTextAsync(cookiePath);
        var entries = JsonSerializer.Deserialize<List<CookieEditorEntry>>(json)
            ?? throw new InvalidOperationException("Cookie file rỗng hoặc không hợp lệ.");

        var cookies = entries.Select(e => new Cookie
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
        }).ToList();

        await context.AddCookiesAsync(cookies);
        Console.WriteLine($"[FB Session] Loaded {cookies.Count} cookies từ {cookiePath}.");
        await TryTouchLastUsedAsync();
    }

    private static async Task<string> ResolveCookiePathAsync()
    {
        try
        {
            return await PlatformCookieRuntime.Provider.ResolveFullPathAsync("facebook")
                   ?? CookiePath;
        }
        catch (InvalidOperationException)
        {
            return CookiePath;
        }
    }

    private static async Task TryTouchLastUsedAsync()
    {
        try
        {
            await PlatformCookieRuntime.Provider.TouchLastUsedAsync("facebook");
        }
        catch (InvalidOperationException)
        {
            // Chưa khởi tạo runtime (unit test / tool) — bỏ qua.
        }
    }
}
