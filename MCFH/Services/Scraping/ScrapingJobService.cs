using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services.Scraping;

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
        {
            await RunSingleProjectAsync(project.ProjectId);
        }

        Console.WriteLine($"[Hangfire] Hoàn tất quét {projects.Count} project");
    }

    private async Task RunSingleProjectAsync(int projectId)
    {
        var jobId = Guid.NewGuid().ToString();

        var job = new ScrapingJob
        {
            JobId = jobId,
            ProjectId = projectId,
            Status = "running",
            StartedAt = DateTime.Now
        };
        _db.ScrapingJobs.Add(job);
        await _db.SaveChangesAsync();

        try
        {
            var result = await _scrapeService.ScrapeAsync(projectId);

            var totalScraped = result.Facebook.Count + result.YouTube.Count + result.TikTok.Count;

            job.Status = result.Errors.Count > 0 ? "completed_with_errors" : "completed";
            job.TotalScraped = totalScraped;
            job.ErrorLog = result.Errors.Count > 0 ? string.Join("; ", result.Errors) : null;
            job.FinishedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.ErrorLog = ex.Message;
            job.FinishedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();
    }
}