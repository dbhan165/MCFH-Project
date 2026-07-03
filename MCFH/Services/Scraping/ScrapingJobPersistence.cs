using MCFH.Models;
using MCFH.Models.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services.Scraping;

/// <summary>Ghi snapshot job vào bảng SCRAPING_JOBS (dùng chung cho API user và Hangfire).</summary>
public static class ScrapingJobPersistence
{
    public static async Task CreateRunningAsync(McfhDbContext db, string jobId, int projectId)
    {
        db.ScrapingJobs.Add(new ScrapingJob
        {
            JobId = jobId,
            ProjectId = projectId,
            Status = "running",
            StartedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
    }

    public static async Task FinalizeAsync(
        McfhDbContext db,
        string jobId,
        string status,
        ScrapeByKeywordResult? result = null,
        string? errorLog = null)
    {
        var job = await db.ScrapingJobs.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job == null)
            return;

        job.Status = status;
        job.TotalScraped = CountSaved(result);
        job.ErrorLog = errorLog ?? BuildErrorLog(result);
        job.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Project đến hạn cào nếu: không có job running (hoặc running đã stale),
    /// và đã qua <paramref name="intervalMinutes"/> kể từ <c>started_at</c> lần gần nhất.
    /// </summary>
    public static async Task<bool> IsProjectDueForScrapeAsync(
        McfhDbContext db,
        int projectId,
        int intervalMinutes,
        int staleRunningMinutes,
        CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var staleCutoff = now.AddMinutes(-staleRunningMinutes);

        var hasActiveRun = await db.ScrapingJobs.AnyAsync(
            j => j.ProjectId == projectId
                 && j.Status == "running"
                 && j.StartedAt != null
                 && j.StartedAt > staleCutoff,
            ct);

        if (hasActiveRun)
            return false;

        var lastStarted = await db.ScrapingJobs
            .Where(j => j.ProjectId == projectId && j.StartedAt != null)
            .MaxAsync(j => (DateTime?)j.StartedAt, ct);

        if (lastStarted == null)
            return true;

        return lastStarted.Value.AddMinutes(intervalMinutes) <= now;
    }

    public static async Task FinalizeFromStateAsync(McfhDbContext db, ScrapeJobState state)
    {
        var status = MapStatus(state);
        await FinalizeAsync(db, state.JobId, status, state.Result, BuildErrorLog(state));
    }

    public static string MapStatus(ScrapeJobState state)
    {
        if (state.Status == "cancelled")
            return "cancelled";

        if (state.Status == "failed")
            return "failed";

        var result = state.Result;
        if (result != null && result.Errors.Count > 0)
            return "completed_with_errors";

        if (!string.IsNullOrWhiteSpace(state.ErrorMessage) || !string.IsNullOrWhiteSpace(result?.ErrorMessage))
            return "failed";

        return "completed";
    }

    public static string MapHangfireStatus(ScrapeByKeywordResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && CountSaved(result) == 0)
            return "failed";

        if (result.Errors.Count > 0)
            return "completed_with_errors";

        return "completed";
    }

    private static int CountSaved(ScrapeByKeywordResult? result) =>
        result == null
            ? 0
            : result.Facebook.Count + result.YouTube.Count + result.TikTok.Count + result.News.Count;

    private static string? BuildErrorLog(ScrapeByKeywordResult? result)
    {
        if (result == null)
            return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            parts.Add(result.ErrorMessage);
        if (result.Errors.Count > 0)
            parts.AddRange(result.Errors);

        return parts.Count > 0 ? string.Join("; ", parts.Distinct()) : null;
    }

    private static string? BuildErrorLog(ScrapeJobState state)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
            parts.Add(state.ErrorMessage);

        if (state.Result != null)
        {
            if (!string.IsNullOrWhiteSpace(state.Result.ErrorMessage))
                parts.Add(state.Result.ErrorMessage);
            parts.AddRange(state.Result.Errors);
        }

        return parts.Count > 0 ? string.Join("; ", parts.Distinct()) : null;
    }
}
