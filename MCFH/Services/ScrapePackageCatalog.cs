using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MCFH.Services;

/// <summary>
/// Read-through cache cho bảng SCRAPE_PACKAGES — lookup theo Code.
/// ScrapeOrderService / PackagesController đọc từ đây thay vì query DB mỗi request.
/// Admin CRUD sẽ gọi <see cref="Invalidate"/> sau khi thêm/sửa/xóa để cache tự refresh.
/// </summary>
public class ScrapePackageCatalog
{
    private const string CacheKey = "scrape-packages:active";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public ScrapePackageCatalog(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<List<ScrapePackage>> GetAllActiveAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<ScrapePackage>? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();

        var packages = await db.ScrapePackages.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PackageId)
            .ToListAsync();

        _cache.Set(CacheKey, packages, CacheTtl);
        return packages;
    }

    public async Task<ScrapePackage?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var all = await GetAllActiveAsync();
        var normalized = code.Trim().ToUpperInvariant();
        return all.FirstOrDefault(p =>
            string.Equals(p.Code, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Lookup 1 phát — trả null nếu code không tồn tại hoặc is_active=false.</summary>
    public async Task<ScrapePackage?> GetActiveByCodeAsync(string code) =>
        await GetByCodeAsync(code);

    public void Invalidate() => _cache.Remove(CacheKey);
}
