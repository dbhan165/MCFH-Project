using MCFH.Configuration;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// UC-78: Chọn proxy active (LRU), gán vào SCRAPING_JOBS, cập nhật fail_count / status.
/// </summary>
public class ProxyRotationService
{
    private readonly McfhDbContext _db;
    private readonly ProxyOptions _options;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public ProxyRotationService(McfhDbContext db, IOptions<ProxyOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public bool IsEnabled => _options.Enabled;

    public static Proxy? ToPlaywrightProxy(SystemProxy? proxy)
    {
        if (proxy == null)
            return null;

        return new Proxy
        {
            Server = BuildServerUrl(proxy),
            Username = string.IsNullOrWhiteSpace(proxy.AuthUser) ? null : proxy.AuthUser,
            Password = string.IsNullOrWhiteSpace(proxy.AuthPass) ? null : proxy.AuthPass
        };
    }

    public static string BuildServerUrl(SystemProxy proxy) =>
        $"http://{proxy.IpAddress.Trim()}:{proxy.Port}";

    /// <summary>Lấy proxy active ít dùng nhất; null nếu tắt rotation hoặc pool rỗng.</summary>
    public async Task<SystemProxy?> AcquireNextAsync(int? excludeProxyId = null, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return null;

        await Gate.WaitAsync(ct);
        try
        {
            var query = _db.SystemProxies.AsQueryable()
                .Where(p => p.Status == "active");

            if (excludeProxyId.HasValue)
                query = query.Where(p => p.ProxyId != excludeProxyId.Value);

            // NULL last_used_at = chưa dùng → ưu tiên trước; tránh DateTime.MinValue (năm 0001, ngoài phạm vi SQL datetime).
            var proxy = await query
                .OrderBy(p => p.LastUsedAt.HasValue ? 1 : 0)
                .ThenBy(p => p.LastUsedAt)
                .ThenBy(p => p.ProxyId)
                .FirstOrDefaultAsync(ct);

            if (proxy == null)
            {
                Console.WriteLine("[Proxy] Không có proxy active — cào trực tiếp (direct).");
                return null;
            }

            proxy.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            Console.WriteLine($"[Proxy] Chọn proxy #{proxy.ProxyId} {proxy.IpAddress}:{proxy.Port}");
            return proxy;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task AssignToJobAsync(string jobId, int proxyId, CancellationToken ct = default)
    {
        var job = await _db.ScrapingJobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct);
        if (job == null)
            return;

        job.ProxyId = proxyId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordSuccessAsync(int proxyId, CancellationToken ct = default)
    {
        var proxy = await _db.SystemProxies.FindAsync(new object[] { proxyId }, ct);
        if (proxy == null || proxy.Status == "dead")
            return;

        proxy.FailCount = 0;
        if (proxy.Status != "active")
            proxy.Status = "active";

        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordFailureAsync(int proxyId, CancellationToken ct = default)
    {
        var proxy = await _db.SystemProxies.FindAsync(new object[] { proxyId }, ct);
        if (proxy == null)
            return;

        proxy.FailCount = (proxy.FailCount ?? 0) + 1;
        if (proxy.FailCount >= _options.MaxFailBeforeDead)
        {
            proxy.Status = "dead";
            Console.WriteLine($"[Proxy] Proxy #{proxyId} đánh dấu dead (fail_count={proxy.FailCount}).");
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SystemProxy?> RotateAsync(
        SystemProxy? current,
        string? jobId,
        bool markCurrentAsFailed,
        CancellationToken ct = default)
    {
        if (current != null)
        {
            if (markCurrentAsFailed)
                await RecordFailureAsync(current.ProxyId, ct);
            else
                await RecordSuccessAsync(current.ProxyId, ct);
        }

        var next = await AcquireNextAsync(current?.ProxyId, ct);
        if (next != null && !string.IsNullOrWhiteSpace(jobId))
            await AssignToJobAsync(jobId, next.ProxyId, ct);

        return next;
    }
}
