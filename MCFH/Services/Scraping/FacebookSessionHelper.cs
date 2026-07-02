using Microsoft.Playwright;
using MCFH.Models.Scraping;
using System.Text.Json;

namespace MCFH.Services.Scraping;

public static class FacebookSessionHelper
{
    public static string CookiePath => ScrapeCookiePaths.FacebookCookiePath;

    public static bool CookieFileExists() => File.Exists(CookiePath);

    public static async Task LoadCookiesAsync(IBrowserContext context)
    {
        if (!CookieFileExists())
            throw new FileNotFoundException($"Cookie file không tồn tại tại: {CookiePath}");

        var json = await File.ReadAllTextAsync(CookiePath);
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
        Console.WriteLine($"[FB Session] Loaded {cookies.Count} cookies.");
    }
}