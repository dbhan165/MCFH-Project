using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services.Scraping;

public class ScrapeJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScrapeJobStore _store;

    public ScrapeJobRunner(IServiceScopeFactory scopeFactory, ScrapeJobStore store)
    {
        _scopeFactory = scopeFactory;
        _store = store;
    }

    public async Task<string?> StartAsync(int projectId, int userId, int? postedSinceDays = null, bool fastDemo = false)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();

        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.IsDeleted != true);

        if (project?.WorkspaceId == null)
            return null;

        // Chặn cào nếu workspace đã bị xóa mềm.
        var workspaceActive = await db.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.WorkspaceId == project.WorkspaceId && w.IsDeleted != true);

        if (!workspaceActive)
            return null;

        var isMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);

        if (!isMember)
            return null;

        var jobId = Guid.NewGuid().ToString("N");
        _store.Create(jobId, projectId, userId, postedSinceDays, fastDemo);
        await ScrapingJobPersistence.CreateRunningAsync(db, jobId, projectId);

        _ = Task.Run(() => ExecuteAsync(jobId, projectId, postedSinceDays, fastDemo));

        return jobId;
    }

    public ScrapeJobState? GetJob(string jobId, int userId)
    {
        var job = _store.Get(jobId);
        if (job == null || job.UserId != userId)
            return null;
        return job;
    }

    public bool CancelJob(string jobId, int userId) => _store.RequestCancel(jobId, userId);

    private async Task ExecuteAsync(string jobId, int projectId, int? postedSinceDays, bool fastDemo)
    {
        var progress = new ScrapeJobProgress(_store, jobId);
        var state = _store.Get(jobId);
        if (state == null)
            return;

        try
        {
            progress.SetPhase("starting", "Đang khởi động bot cào dữ liệu...");

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ScrapeByKeywordService>();
            var result = await service.ScrapeAsync(
                projectId, progress, ScrapeTimeFilter.FromDays(postedSinceDays), fastDemo, jobId);

            if (state.IsCancellationRequested)
                state.CompleteCancelled(result);
            else
                state.Complete(result);
        }
        catch (Exception ex)
        {
            state.Fail(ex.Message);
        }
        finally
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
                await ScrapingJobPersistence.FinalizeFromStateAsync(db, state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScrapeJob] Không cập nhật SCRAPING_JOBS #{jobId}: {ex.Message}");
            }
        }
    }
}
