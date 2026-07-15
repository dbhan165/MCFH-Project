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

                if (state.Status == "completed")
                {
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId);
                    if (project != null)
                    {
                        var totalFetched = (state.Result?.Facebook?.Count ?? 0) +
                                           (state.Result?.YouTube?.Count ?? 0) +
                                           (state.Result?.TikTok?.Count ?? 0) +
                                           (state.Result?.News?.Count ?? 0);

                        await notifications.NotifyAsync(
                            userId: state.UserId,
                            title: "Hoàn tất thu thập dữ liệu",
                            message: $"Đã cào xong dữ liệu cho dự án «{project.Name}». Có {totalFetched} mention mới.",
                            type: "scrape_completed",
                            relatedType: "project",
                            relatedId: projectId,
                            projectId: projectId
                        );
                    }
                }
                else if (state.Status == "failed")
                {
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId);
                    var projectName = project?.Name ?? "Dự án";
                    var errorMsg = state.ErrorMessage ?? "Lỗi không xác định";

                    var adminIds = await db.Users.Where(u => u.SystemRole == "admin" || u.SystemRole == "system_admin").Select(u => u.UserId).ToListAsync();
                    
                    foreach (var adminId in adminIds)
                    {
                        await notifications.NotifyAsync(
                            userId: adminId,
                            title: "Lỗi cào dữ liệu (Thủ công)",
                            message: $"Dự án «{projectName}» cào thất bại. User: {state.UserId}. Chi tiết: {errorMsg}",
                            type: "scrape_failed",
                            relatedType: "project",
                            relatedId: projectId,
                            projectId: projectId
                        );
                    }

                    if (!adminIds.Contains(state.UserId))
                    {
                        await notifications.NotifyAsync(
                            userId: state.UserId,
                            title: "Cào dữ liệu thất bại",
                            message: $"Tiến trình cào cho dự án «{projectName}» gặp lỗi. Admin đã được thông báo để xử lý.",
                            type: "scrape_failed",
                            relatedType: "project",
                            relatedId: projectId,
                            projectId: projectId
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScrapeJob] Không cập nhật SCRAPING_JOBS #{jobId}: {ex.Message}");
            }
        }
    }
}
