using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Models.Scraping;
using MCFH.Services;

namespace MCFH.Services.Scraping;

public static class ThreadsSessionHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string CookiePath => ScrapeCookiePaths.ThreadsCookiePath;

    public static bool CookieFileExists(string? cookieFilePath = null) =>
        File.Exists(cookieFilePath ?? CookiePath);

    /// <summary>
    /// Load cookies từ file JSON vào Playwright context.
    /// Threads yêu cầu đăng nhập để xem comments, nên ném nếu không có cookie.
    /// </summary>
    public static async Task LoadCookiesAsync(IBrowserContext context)
    {
        var cookiePath = await ResolveCookiePathAsync();
        if (!File.Exists(cookiePath))
            throw new FileNotFoundException(
                $"Threads cookie file không tồn tại: {cookiePath}. " +
                "Vui lòng xuất cookie từ threads.com bằng Cookie-Editor và lưu vào cookies/threads_cookie.json");

        var json = await File.ReadAllTextAsync(cookiePath);
        var entries = JsonSerializer.Deserialize<List<CookieEditorEntry>>(json)
            ?? throw new InvalidOperationException("Threads cookie file rỗng hoặc không hợp lệ.");

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
        Console.WriteLine($"[Threads Session] Loaded {cookies.Count} cookies từ {cookiePath}.");
        await TryTouchLastUsedAsync();
    }

    private static async Task<string> ResolveCookiePathAsync()
    {
        try
        {
            return await PlatformCookieRuntime.Provider.ResolveFullPathAsync("threads")
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
            await PlatformCookieRuntime.Provider.TouchLastUsedAsync("threads");
        }
        catch (InvalidOperationException)
        {
            // Chưa khởi tạo runtime — bỏ qua.
        }
    }

    public static async Task SaveCookiesAsync(IBrowserContext context)
    {
        try
        {
            var cookiePath = await ResolveCookiePathAsync();
            var cookies = await context.CookiesAsync();
            var threadsCookies = cookies
                .Where(c => c.Domain.Contains("threads", StringComparison.OrdinalIgnoreCase) ||
                            c.Domain.Contains("instagram", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (threadsCookies.Count == 0)
            {
                Console.WriteLine("[Threads Session] No threads/instagram cookies to save.");
                return;
            }

            if (File.Exists(cookiePath))
            {
                var backupPath = await ResolveBackupPathAsync(cookiePath);
                File.Copy(cookiePath, backupPath, overwrite: true);
                Console.WriteLine($"[Threads Session] Backup cookie → {backupPath}");
            }

            var entries = threadsCookies.Select(ToCookieEditorEntry).ToList();
            await File.WriteAllTextAsync(cookiePath, JsonSerializer.Serialize(entries, JsonOptions));
            Console.WriteLine($"[Threads Session] Saved {entries.Count} cookies → {cookiePath}");
            await TryTouchLastUsedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Threads Session] Cookie save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Chỉ lưu khi phiên scrape thành công — tránh session bị chặn ghi đè cookie tốt trên server.
    /// </summary>
    public static async Task TrySaveAfterSuccessfulSessionAsync(IBrowserContext? context, int newPostsSaved)
    {
        if (context == null)
            return;

        if (newPostsSaved <= 0)
        {
            Console.WriteLine("[Threads Session] Skip save — không lưu được bài mới trong phiên này.");
            return;
        }

        await SaveCookiesAsync(context);
    }

    private static async Task<string> ResolveBackupPathAsync(string cookiePath)
    {
        try
        {
            var provider = PlatformCookieRuntime.Provider;
            var relative = Path.GetRelativePath(provider.ContentRoot, cookiePath).Replace('\\', '/');
            var backupRelative = provider.GetBackupRelativePath("threads", relative);
            return provider.ToFullPath(backupRelative);
        }
        catch (InvalidOperationException)
        {
            return Path.Combine(Path.GetDirectoryName(cookiePath) ?? "", "threads_cookie.backup.json");
        }
    }

    private static CookieEditorEntry ToCookieEditorEntry(BrowserContextCookiesResult c) => new()
    {
        Name = c.Name,
        Value = c.Value,
        Domain = c.Domain ?? ".threads.net",
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
