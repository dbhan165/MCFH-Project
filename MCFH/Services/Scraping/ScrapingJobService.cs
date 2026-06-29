using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services.Scraping;

/// <summary>Hangfire — quét định kỳ mọi project active, dùng chung pipeline ScrapeByKeywordService.</summary>
public class ScrapingJobService
{
    private readonly McfhDbContext _db;
    private readonly ScrapeByKeywordService _scrapeService;

    public ScrapingJobService(McfhDbContext db, ScrapeByKeywordService scrapeService)
    {
        _db = db;
        _scrapeService = scrapeService;
    }

    public async Task RunAllProjectsAsync()
    {
        var projects = await _db.Projects
            .Where(p => p.IsDeleted != true
                && (p.EnableFacebook == true || p.EnableYoutube == true || p.EnableTiktok == true))
            .ToListAsync();

        Console.WriteLine($"[Hangfire] Bắt đầu quét {projects.Count} project active");

        foreach (var project in projects)
            await RunSingleProjectAsync(project.ProjectId);

        Console.WriteLine($"[Hangfire] Hoàn tất quét {projects.Count} project");
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
