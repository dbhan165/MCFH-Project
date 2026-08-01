using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>Admin CRUD — cấu hình các gói scrape (bảng SCRAPE_PACKAGES).</summary>
public class ScrapePackageAdminService
{
    private readonly McfhDbContext _context;
    private readonly ScrapePackageCatalog _catalog;

    public ScrapePackageAdminService(McfhDbContext context, ScrapePackageCatalog catalog)
    {
        _context = context;
        _catalog = catalog;
    }

    public async Task<List<ScrapePackageDto>> ListAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId))
            return new();

        return await _context.ScrapePackages
            .AsNoTracking()
            .Include(p => p.UpdatedByNavigation)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PackageId)
            .Select(p => new ScrapePackageDto
            {
                PackageId = p.PackageId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                MaxItems = p.MaxItems,
                MaxSources = p.MaxSources,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy,
                UpdatedByName = p.UpdatedByNavigation != null ? p.UpdatedByNavigation.FullName : null,
                ActiveOrdersCount = p.ScrapeOrders.Count(o => o.Status != "cancelled")
            })
            .ToListAsync();
    }

    public async Task<ScrapePackageDto?> CreateAsync(int adminUserId, UpsertScrapePackageDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var (ok, msg) = await Validate(dto, excludePackageId: 0);
        if (!ok)
            throw new ArgumentException(msg);

        var code = dto.Code.Trim().ToUpperInvariant();
        var entity = new ScrapePackage
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Price = dto.Price,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant(),
            DurationDays = dto.DurationDays,
            MaxItems = dto.MaxItems,
            MaxSources = dto.MaxSources,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UpdatedBy = adminUserId
        };

        _context.ScrapePackages.Add(entity);
        await _context.SaveChangesAsync();
        _catalog.Invalidate();

        return await GetDtoAsync(entity.PackageId);
    }

    public async Task<ScrapePackageDto?> UpdateAsync(int adminUserId, int packageId, UpsertScrapePackageDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var entity = await _context.ScrapePackages.FindAsync(packageId);
        if (entity == null)
            return null;

        var (ok, msg) = await Validate(dto, excludePackageId: packageId);
        if (!ok)
            throw new ArgumentException(msg);

        entity.Code = dto.Code.Trim().ToUpperInvariant();
        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.Price = dto.Price;
        entity.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant();
        entity.DurationDays = dto.DurationDays;
        entity.MaxItems = dto.MaxItems;
        entity.MaxSources = dto.MaxSources;
        entity.IsActive = dto.IsActive;
        entity.SortOrder = dto.SortOrder;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = adminUserId;

        await _context.SaveChangesAsync();
        _catalog.Invalidate();

        return await GetDtoAsync(packageId);
    }

    public async Task<bool> DeleteAsync(int adminUserId, int packageId)
    {
        if (!await IsAdminAsync(adminUserId))
            return false;

        var entity = await _context.ScrapePackages
            .Include(p => p.ScrapeOrders)
            .FirstOrDefaultAsync(p => p.PackageId == packageId);

        if (entity == null)
            return false;

        if (entity.ScrapeOrders.Any())
            throw new InvalidOperationException(
                $"Không thể xóa gói '{entity.Code}' vì đang có {entity.ScrapeOrders.Count} đơn hàng tham chiếu.");

        _context.ScrapePackages.Remove(entity);
        await _context.SaveChangesAsync();
        _catalog.Invalidate();
        return true;
    }

    private async Task<ScrapePackageDto?> GetDtoAsync(int packageId) =>
        await _context.ScrapePackages
            .AsNoTracking()
            .Include(p => p.UpdatedByNavigation)
            .Where(p => p.PackageId == packageId)
            .Select(p => new ScrapePackageDto
            {
                PackageId = p.PackageId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                MaxItems = p.MaxItems,
                MaxSources = p.MaxSources,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy,
                UpdatedByName = p.UpdatedByNavigation != null ? p.UpdatedByNavigation.FullName : null,
                ActiveOrdersCount = p.ScrapeOrders.Count(o => o.Status != "cancelled")
            })
            .FirstOrDefaultAsync();

    private async Task<(bool ok, string msg)> Validate(UpsertScrapePackageDto dto, int excludePackageId)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return (false, "Mã gói (Code) là bắt buộc.");
        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "Tên gói là bắt buộc.");
        if (dto.Price < 0)
            return (false, "Giá phải >= 0.");
        if (dto.DurationDays <= 0)
            return (false, "Số ngày phải > 0.");
        if (dto.MaxItems <= 0)
            return (false, "Số mentions tối đa phải > 0.");

        var code = dto.Code.Trim().ToUpperInvariant();

        // Cap maxSources theo loại gói:
        //   - Gói FULL_* (toàn diện): cho phép tối đa 99 (future-proof khi có thêm platform)
        //   - Gói thường: tối đa 6 (số platform scrape hiện tại trong SCRAPABLE_PLATFORMS)
        if (dto.MaxSources.HasValue)
        {
            var maxSourcesCap = code.StartsWith("FULL", StringComparison.OrdinalIgnoreCase) ? 99 : 6;
            if (dto.MaxSources.Value < 0)
                return (false, "Số nguồn phải >= 0.");
            if (dto.MaxSources.Value > maxSourcesCap)
                return (false, $"Số nguồn tối đa là {maxSourcesCap} cho {(code.StartsWith("FULL", StringComparison.OrdinalIgnoreCase) ? "gói toàn diện (FULL_*)" : "gói thường")}.");
        }

        var conflict = await _context.ScrapePackages
            .Where(p => p.PackageId != excludePackageId && p.Code == code)
            .Select(p => p.PackageId)
            .FirstOrDefaultAsync();
        if (conflict != 0)
            return (false, $"Mã '{code}' đã tồn tại.");

        return (true, string.Empty);
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        if (userId <= 0) return false;
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
