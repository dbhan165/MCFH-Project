using Hangfire;
using MCFH.Configuration;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MCFH.Services.Scraping;

/// <summary>
/// Hangfire — lên lịch cào theo từng project (UC-76).
/// Căn cứ SCRAPING_JOBS.started_at: mỗi project cách lần bắt đầu trước ≥ PerProjectScrapeIntervalMinutes.
/// </summary>
public class ScrapingJobService
{
    private readonly McfhDbContext _db;
    private readonly ScrapeByKeywordService _scrapeService;
    private readonly ScrapeOptions _scrapeOptions;

    public ScrapingJobService(
        McfhDbContext db,
        ScrapeByKeywordService scrapeService,
        IOptions<ScrapeOptions> scrapeOptions)
    {
        _db = db;
        _scrapeService = scrapeService;
        _scrapeOptions = scrapeOptions.Value;
    }

    /// <summary>
    /// Tick scheduler (cron thường mỗi phút): chỉ chạy project đã đến hạn, không quét tuần tự cả pool.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 30)]
    public async Task RunDueProjectsAsync()
    {
        var interval = _scrapeOptions.PerProjectScrapeIntervalMinutes;
        var stale = _scrapeOptions.StaleRunningJobMinutes;

        var projectIds = await _db.Projects
            .Where(p => p.IsDeleted != true
                        && (p.EnableFacebook == true || p.EnableYoutube == true || p.EnableTiktok == true
                            || p.DataSources.Any(d => d.Platform == "news" && d.Status == "active")))
            .Select(p => p.ProjectId)
            .ToListAsync();

        var dueIds = new List<int>();
        foreach (var projectId in projectIds)
        {
            if (await ScrapingJobPersistence.IsProjectDueForScrapeAsync(_db, projectId, interval, stale))
                dueIds.Add(projectId);
        }

        if (dueIds.Count == 0)
        {
            Console.WriteLine($"[Hangfire] Scheduler: 0/{projectIds.Count} project đến hạn (interval {interval} phút từ started_at).");
            return;
        }

        Console.WriteLine($"[Hangfire] Scheduler: {dueIds.Count}/{projectIds.Count} project đến hạn — bắt đầu cào.");

        foreach (var projectId in dueIds)
            await RunSingleProjectAsync(projectId);

        Console.WriteLine($"[Hangfire] Scheduler: hoàn tất {dueIds.Count} project.");
    }

    private async Task RunSingleProjectAsync(int projectId)
    {
        var jobId = Guid.NewGuid().ToString("N");

        await ScrapingJobPersistence.CreateRunningAsync(_db, jobId, projectId);

        try
        {
            var result = await _scrapeService.ScrapeAsync(projectId, scrapeJobId: jobId);
            var status = ScrapingJobPersistence.MapHangfireStatus(result);
            await ScrapingJobPersistence.FinalizeAsync(_db, jobId, status, result);
        }
        catch (Exception ex)
        {
            await ScrapingJobPersistence.FinalizeAsync(_db, jobId, "failed", errorLog: ex.Message);
        }
    }
}
