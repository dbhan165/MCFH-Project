using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// Read-only service cho client — trả danh sách package scrape đang active.
/// Dùng cho trang "Mua gói" trên frontend (chưa đăng nhập cũng xem được).
/// </summary>
public class ScrapePackagePublicService
{
    private readonly McfhDbContext _context;

    public ScrapePackagePublicService(McfhDbContext context)
    {
        _context = context;
    }

    public Task<List<PublicScrapePackageDto>> ListActiveAsync() =>
        _context.ScrapePackages.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PackageId)
            .Select(p => new PublicScrapePackageDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                MaxItems = p.MaxItems,
                MaxSources = p.MaxSources,
                SortOrder = p.SortOrder
            })
            .ToListAsync();
}
