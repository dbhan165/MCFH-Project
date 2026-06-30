using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// Truy vấn SCRAPED_FEEDBACKS theo project_id trực tiếp hoặc qua DATA_SOURCES.source_id.
/// </summary>
public static class ProjectFeedbackQueryHelper
{
    public static IQueryable<ScrapedFeedback> ForProject(McfhDbContext db, int projectId) =>
        db.ScrapedFeedbacks.Where(f =>
            f.IsDeleted != true &&
            (f.ProjectId == projectId ||
             (f.SourceId != null &&
              db.DataSources.Any(d => d.SourceId == f.SourceId && d.ProjectId == projectId))));

    public static async Task BackfillProjectIdsAsync(McfhDbContext db, int projectId)
    {
        var orphans = await (
            from f in db.ScrapedFeedbacks
            join d in db.DataSources on f.SourceId equals d.SourceId
            where f.ProjectId == null && d.ProjectId == projectId && f.IsDeleted != true
            select new { Feedback = f, d.Platform }
        ).ToListAsync();

        if (orphans.Count == 0) return;

        foreach (var row in orphans)
        {
            row.Feedback.ProjectId = projectId;
            if (string.IsNullOrWhiteSpace(row.Feedback.Platform))
                row.Feedback.Platform = row.Platform;
        }

        await db.SaveChangesAsync();
    }
}
