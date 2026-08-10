using System.Collections.Concurrent;
using System.Text.Json;
using MCFH.DTOs;
using MCFH.Models;
using MCFH.Models.Scraping;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public interface IPlatformCookiePathProvider
{
    string ContentRoot { get; }
    Task<string?> ResolveFullPathAsync(string platform, CancellationToken ct = default);
    Task TouchLastUsedAsync(string platform, CancellationToken ct = default);
    void InvalidateCache();
    string GetBackupRelativePath(string platform, string relativeFilePath);
    bool IsRelativePathAllowed(string relativePath);
    string ToFullPath(string relativePath);
}

/// <summary>Đọc file_path từ PLATFORM_COOKIES, cache ngắn hạn, fallback path mặc định.</summary>
public class PlatformCookiePathProvider : IPlatformCookiePathProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, (string? Path, DateTime Expires)> _cache = new();

    public PlatformCookiePathProvider(IWebHostEnvironment env, IServiceScopeFactory scopeFactory)
    {
        ContentRoot = env.ContentRootPath;
        _scopeFactory = scopeFactory;
    }

    public string ContentRoot { get; }

    public async Task<string?> ResolveFullPathAsync(string platform, CancellationToken ct = default)
    {
        platform = NormalizePlatform(platform);
        if (_cache.TryGetValue(platform, out var cached) && cached.Expires > DateTime.UtcNow)
            return cached.Path;

        string? fullPath = null;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
            var row = await db.PlatformCookies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Platform == platform, ct);

            if (row != null && row.Status == "active" && IsRelativePathAllowed(row.FilePath))
                fullPath = ToFullPath(row.FilePath);
        }

        fullPath ??= GetDefaultFullPath(platform);
        _cache[platform] = (fullPath, DateTime.UtcNow.Add(CacheTtl));
        return fullPath;
    }

    public async Task TouchLastUsedAsync(string platform, CancellationToken ct = default)
    {
        platform = NormalizePlatform(platform);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
        var row = await db.PlatformCookies.FirstOrDefaultAsync(p => p.Platform == platform, ct);
        if (row == null)
            return;

        row.LastUsedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
    }

    public void InvalidateCache() => _cache.Clear();

    public string GetBackupRelativePath(string platform, string relativeFilePath)
    {
        platform = NormalizePlatform(platform);
        var fileName = Path.GetFileName(relativeFilePath);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        return Path.Combine("cookies", $"{name}.backup{ext}").Replace('\\', '/');
    }

    public bool IsRelativePathAllowed(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        relativePath = relativePath.Replace('\\', '/').Trim();
        if (relativePath.StartsWith('/') || relativePath.Contains("..", StringComparison.Ordinal))
            return false;

        var full = Path.GetFullPath(Path.Combine(ContentRoot, relativePath));
        var cookiesRoot = Path.GetFullPath(Path.Combine(ContentRoot, "cookies"));
        return full.StartsWith(cookiesRoot, StringComparison.OrdinalIgnoreCase);
    }

    public string ToFullPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(ContentRoot, relativePath.Replace('\\', '/')));

    private string? GetDefaultFullPath(string platform) => platform switch
    {
        "facebook" => ScrapeCookiePaths.FacebookCookiePath,
        "tiktok" => ScrapeCookiePaths.TikTokCookiePath,
        "threads" => ScrapeCookiePaths.ThreadsCookiePath,
        _ => null
    };

    private static string NormalizePlatform(string platform) =>
        platform.Trim().ToLowerInvariant();
}

public static class PlatformCookieRuntime
{
    private static IPlatformCookiePathProvider? _provider;

    public static void Initialize(IPlatformCookiePathProvider provider) =>
        _provider = provider;

    public static IPlatformCookiePathProvider Provider =>
        _provider ?? throw new InvalidOperationException("PlatformCookieRuntime chưa được khởi tạo.");
}

public static class PlatformCookieFileHelper
{
  private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };

    public static readonly string[] FacebookRequired = ["c_user", "xs"];
    public static readonly string[] TikTokRequired = ["sessionid", "sid_tt"];
    public static readonly string[] ThreadsRequired = ["auth_token", "ds_user_id"];

    public static string NormalizePlatform(string platform)
    {
        var p = platform.Trim().ToLowerInvariant();
        if (p is not ("facebook" or "tiktok" or "threads"))
            throw new ArgumentException("Platform phải là 'facebook', 'tiktok', hoặc 'threads'.");
        return p;
    }

    public static List<CookieEditorEntry> ParseCookies(UpdatePlatformCookieContentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CookiesJson))
            throw new ArgumentException("cookiesJson là bắt buộc.");

        List<CookieEditorEntry>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<CookieEditorEntry>>(dto.CookiesJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"JSON cookie không hợp lệ: {ex.Message}");
        }

        if (entries == null || entries.Count == 0)
            throw new ArgumentException("Mảng cookie rỗng.");

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Domain))
                throw new ArgumentException("Mỗi cookie phải có name và domain.");
        }

        return entries;
    }

    public static void ValidateRequiredCookies(string platform, List<CookieEditorEntry> entries)
    {
        var names = entries.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (platform == "facebook")
        {
            foreach (var name in FacebookRequired)
            {
                if (!names.Contains(name))
                    throw new ArgumentException($"Facebook cookie thiếu '{name}'.");
            }
        }
        else if (platform == "tiktok")
        {
            if (!names.Contains("sessionid") && !names.Contains("sid_tt"))
                throw new ArgumentException("TikTok cookie thiếu sessionid hoặc sid_tt.");
        }
        else if (platform == "threads")
        {
            if (!names.Contains("auth_token") && !names.Contains("ds_user_id"))
                throw new ArgumentException("Threads cookie thiếu auth_token hoặc ds_user_id.");
        }
    }

    public static Dictionary<string, bool> GetRequiredPresence(string platform, List<CookieEditorEntry>? entries)
    {
        var names = entries?.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return platform switch
        {
            "facebook" => FacebookRequired.ToDictionary(n => n, n => names.Contains(n)),
            "tiktok" => new Dictionary<string, bool>
            {
                ["sessionid"] = names.Contains("sessionid"),
                ["sid_tt"] = names.Contains("sid_tt")
            },
            "threads" => new Dictionary<string, bool>
            {
                ["auth_token"] = names.Contains("auth_token"),
                ["ds_user_id"] = names.Contains("ds_user_id")
            },
            _ => new Dictionary<string, bool>()
        };
    }

    public static DateTime? ComputeExpiresAt(List<CookieEditorEntry> entries)
    {
        var expiring = entries
            .Where(e => e.ExpirationDate is > 0)
            .Select(e => DateTimeOffset.FromUnixTimeSeconds((long)e.ExpirationDate!.Value).UtcDateTime)
            .ToList();

        return expiring.Count == 0 ? null : expiring.Min();
    }

    public static async Task WriteCookieFileAsync(string fullPath, List<CookieEditorEntry> entries)
    {
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(entries, JsonWriteOptions);
        await File.WriteAllTextAsync(fullPath, json);
    }

    public static async Task<List<CookieEditorEntry>?> TryReadCookieFileAsync(string fullPath)
    {
        if (!File.Exists(fullPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(fullPath);
            return JsonSerializer.Deserialize<List<CookieEditorEntry>>(json);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsExpiringSoon(DateTime? expiresAt) =>
        expiresAt.HasValue &&
        expiresAt.Value > DateTime.Now &&
        expiresAt.Value <= DateTime.Now.AddDays(7);

    public static bool IsExpired(DateTime? expiresAt) =>
        expiresAt.HasValue && expiresAt.Value < DateTime.Now;
}
