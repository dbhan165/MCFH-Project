using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>Admin CRUD — nguồn Facebook group/page toàn hệ thống (bảng FB_SOURCES).</summary>
public class FbSourceAdminService
{
    private readonly McfhDbContext _context;

    public FbSourceAdminService(McfhDbContext context)
    {
        _context = context;
    }

    public async Task<List<FbSourceDto>> ListAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId))
            return new();

        return await _context.FbSources
            .Include(s => s.AddedByNavigation)
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.FbSourceId)
            .Select(s => new FbSourceDto
            {
                FbSourceId = s.FbSourceId,
                GroupUrl = s.GroupUrl,
                GroupName = s.GroupName,
                Status = s.Status,
                AddedBy = s.AddedBy,
                AddedByName = s.AddedByNavigation.FullName,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<FbSourceDto?> CreateAsync(int adminUserId, UpsertFbSourceDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var url = NormalizeUrl(dto.GroupUrl);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL Facebook không hợp lệ.");

        var source = new FbSource
        {
            GroupUrl = url,
            GroupName = string.IsNullOrWhiteSpace(dto.GroupName) ? null : dto.GroupName.Trim(),
            Status = ResolveStatus(dto),
            AddedBy = adminUserId,
            CreatedAt = DateTime.Now
        };

        _context.FbSources.Add(source);
        await _context.SaveChangesAsync();

        return await GetDtoAsync(source.FbSourceId);
    }

    public async Task<FbSourceDto?> UpdateAsync(int adminUserId, int fbSourceId, UpsertFbSourceDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var source = await _context.FbSources.FindAsync(fbSourceId);
        if (source == null)
            return null;

        var url = NormalizeUrl(dto.GroupUrl);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL Facebook không hợp lệ.");

        source.GroupUrl = url;
        source.GroupName = string.IsNullOrWhiteSpace(dto.GroupName) ? null : dto.GroupName.Trim();
        source.Status = ResolveStatus(dto);
        await _context.SaveChangesAsync();

        return await GetDtoAsync(fbSourceId);
    }

    public async Task<bool> DeleteAsync(int adminUserId, int fbSourceId)
    {
        if (!await IsAdminAsync(adminUserId))
            return false;

        var source = await _context.FbSources.FindAsync(fbSourceId);
        if (source == null)
            return false;

        _context.FbSources.Remove(source);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<FbSourceDto?> GetDtoAsync(int fbSourceId) =>
        await _context.FbSources
            .Include(s => s.AddedByNavigation)
            .Where(s => s.FbSourceId == fbSourceId)
            .Select(s => new FbSourceDto
            {
                FbSourceId = s.FbSourceId,
                GroupUrl = s.GroupUrl,
                GroupName = s.GroupName,
                Status = s.Status,
                AddedBy = s.AddedBy,
                AddedByName = s.AddedByNavigation.FullName,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();

    private static string NormalizeUrl(string raw)
    {
        var normalized = FacebookUrlHelper.NormalizeSourceUrl(raw);
        return FacebookUrlHelper.LooksLikeFacebookUrl(normalized) ? normalized : "";
    }

    private static string ResolveStatus(UpsertFbSourceDto dto)
    {
        if (!dto.Enabled)
            return "disabled";
        if (!string.IsNullOrWhiteSpace(dto.Status))
            return dto.Status.Trim().ToLowerInvariant();
        return "active";
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
