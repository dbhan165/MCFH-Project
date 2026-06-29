using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>UC-70–73: Admin quản lý proxy; UC-74: xem scraping jobs.</summary>
public class ProxyAdminService
{
    private readonly McfhDbContext _context;

    public ProxyAdminService(McfhDbContext context)
    {
        _context = context;
    }

    public async Task<List<SystemProxyDto>> ListProxiesAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId))
            return new();

        return await _context.SystemProxies
            .OrderByDescending(p => p.LastUsedAt)
            .ThenBy(p => p.ProxyId)
            .Select(p => new SystemProxyDto
            {
                ProxyId = p.ProxyId,
                IpAddress = p.IpAddress,
                Port = p.Port,
                AuthUser = p.AuthUser,
                Status = p.Status,
                FailCount = p.FailCount ?? 0,
                LastUsedAt = p.LastUsedAt,
                Enabled = p.Status != "disabled" && p.Status != "dead"
            })
            .ToListAsync();
    }

    public async Task<SystemProxyDto?> CreateProxyAsync(int adminUserId, UpsertSystemProxyDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var proxy = new SystemProxy
        {
            IpAddress = dto.IpAddress.Trim(),
            Port = dto.Port,
            AuthUser = dto.AuthUser,
            AuthPass = dto.AuthPass,
            Status = ResolveStatus(dto),
            FailCount = 0,
            LastUsedAt = null
        };
        _context.SystemProxies.Add(proxy);
        await _context.SaveChangesAsync();

        return Map(proxy);
    }

    public async Task<SystemProxyDto?> UpdateProxyAsync(int adminUserId, int proxyId, UpsertSystemProxyDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        var proxy = await _context.SystemProxies.FindAsync(proxyId);
        if (proxy == null)
            return null;

        proxy.IpAddress = dto.IpAddress.Trim();
        proxy.Port = dto.Port;
        proxy.AuthUser = dto.AuthUser;
        if (!string.IsNullOrWhiteSpace(dto.AuthPass))
            proxy.AuthPass = dto.AuthPass;
        proxy.Status = ResolveStatus(dto);
        await _context.SaveChangesAsync();

        return Map(proxy);
    }

    public async Task<bool> DeleteProxyAsync(int adminUserId, int proxyId)
    {
        if (!await IsAdminAsync(adminUserId))
            return false;

        var proxy = await _context.SystemProxies.FindAsync(proxyId);
        if (proxy == null)
            return false;

        _context.SystemProxies.Remove(proxy);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScrapingJobDto>> ListScrapingJobsAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId))
            return new();

        return await _context.ScrapingJobs
            .Include(j => j.Project)
            .Include(j => j.Proxy)
            .OrderByDescending(j => j.StartedAt)
            .Take(100)
            .Select(j => new ScrapingJobDto
            {
                JobId = j.JobId,
                ProjectId = j.ProjectId,
                ProjectName = j.Project.Name,
                SourceId = j.SourceId,
                Status = j.Status,
                TotalScraped = j.TotalScraped ?? 0,
                ErrorLog = j.ErrorLog,
                ProxyIp = j.Proxy != null ? j.Proxy.IpAddress : null,
                StartedAt = j.StartedAt,
                FinishedAt = j.FinishedAt
            })
            .ToListAsync();
    }

    private static string ResolveStatus(UpsertSystemProxyDto dto)
    {
        if (!dto.Enabled)
            return "disabled";
        if (!string.IsNullOrWhiteSpace(dto.Status))
            return dto.Status.Trim().ToLowerInvariant();
        return "active";
    }

    private static SystemProxyDto Map(SystemProxy proxy) => new()
    {
        ProxyId = proxy.ProxyId,
        IpAddress = proxy.IpAddress,
        Port = proxy.Port,
        AuthUser = proxy.AuthUser,
        Status = proxy.Status,
        FailCount = proxy.FailCount ?? 0,
        LastUsedAt = proxy.LastUsedAt,
        Enabled = proxy.Status is "active"
    };

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
