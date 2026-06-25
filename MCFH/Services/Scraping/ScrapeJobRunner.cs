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

    public async Task<string?> StartAsync(int projectId, int userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();

        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.IsDeleted != true);

        if (project?.WorkspaceId == null)
            return null;

        var isMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);

        if (!isMember)
            return null;

        var jobId = Guid.NewGuid().ToString("N");
        _store.Create(jobId, projectId, userId);

        _ = Task.Run(() => ExecuteAsync(jobId, projectId));

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

    private async Task ExecuteAsync(string jobId, int projectId)
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
            var result = await service.ScrapeAsync(projectId, progress);

            if (state.IsCancellationRequested)
                state.CompleteCancelled(result);
            else
                state.Complete(result);
        }
        catch (Exception ex)
        {
            state?.Fail(ex.Message);
        }
    }
}
