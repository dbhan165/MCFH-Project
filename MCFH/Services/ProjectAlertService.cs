using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class ProjectAlertService
{
    private readonly McfhDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<ProjectAlertService> _logger;

    public ProjectAlertService(
        McfhDbContext context,
        INotificationService notifications,
        ILogger<ProjectAlertService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task NotifyAfterAnalysisAsync(int projectId, int crisisCountInBatch = 0)
    {
        try
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.IsDeleted != true);
            if (project?.WorkspaceId == null) return;

            var feedbacks = await _context.ScrapedFeedbacks
                .AsNoTracking()
                .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
                .Include(f => f.AiAnalysis)
                .ToListAsync();

            var muted = await _context.MutedEntities
                .AsNoTracking()
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();
            var mutedAuthors = new HashSet<string>(muted.Where(m => m.EntityType == "author").Select(m => m.EntityValue), StringComparer.OrdinalIgnoreCase);
            var mutedPlatforms = new HashSet<string>(muted.Where(m => m.EntityType == "platform").Select(m => m.EntityValue), StringComparer.OrdinalIgnoreCase);

            var positive = 0;
            var negative = 0;
            var neutral = 0;
            var crisisTotal = 0;

            foreach (var feedback in feedbacks)
            {
                var author = feedback.AuthorName?.Trim();
                var platform = (feedback.Platform ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(author) && mutedAuthors.Contains(author)) continue;
                if (!string.IsNullOrWhiteSpace(platform) && mutedPlatforms.Contains(platform)) continue;
                var sentiment = feedback.AiAnalysis?.MainSentiment?.ToLowerInvariant();
                switch (sentiment)
                {
                    case "positive": positive++; break;
                    case "negative": negative++; break;
                    case "neutral": neutral++; break;
                }

                if (feedback.AiAnalysis?.IsCrisisAlert == true)
                    crisisTotal++;
            }

            var analyzed = positive + negative + neutral;
            if (analyzed == 0 && crisisCountInBatch == 0) return;

            var negativePercent = analyzed > 0 ? negative * 100.0 / analyzed : 0;
            var nsr = analyzed > 0 ? Math.Round((positive - negative) * 100.0 / analyzed, 1) : 0;
            var negativeTrend = nsr < 0 || negativePercent >= 35;
            var shouldAlert = crisisCountInBatch > 0 || crisisTotal > 0 || negativeTrend;
            if (!shouldAlert) return;

            var memberIds = await _context.WorkspaceMembers
                .Where(m => m.WorkspaceId == project.WorkspaceId)
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync();

            if (memberIds.Count == 0) return;

            var since = DateTime.Now.AddHours(-6);
            var title = crisisCountInBatch > 0 || crisisTotal > 0
                ? "Cảnh báo khủng hoảng truyền thông"
                : "Xu hướng tiêu cực được phát hiện";

            var message = crisisCountInBatch > 0 || crisisTotal > 0
                ? $"Dự án «{project.Name}»: phát hiện {Math.Max(crisisCountInBatch, crisisTotal)} mention khủng hoảng. NSR {nsr:+#0.#;-#0.#;0}%, tiêu cực {negativePercent:0.#}%."
                : $"Dự án «{project.Name}»: NSR {nsr:+#0.#;-#0.#;0}% với {negativePercent:0.#}% mention tiêu cực. Nên xem tab Sentiment.";

            foreach (var userId in memberIds)
            {
                var alreadySent = await _context.Notifications.AnyAsync(n =>
                    n.UserId == userId &&
                    n.ProjectId == projectId &&
                    n.Type == "crisis_alert" &&
                    n.CreatedAt >= since);

                if (alreadySent) continue;

                await _notifications.NotifyAsync(
                    userId,
                    title,
                    message,
                    "crisis_alert",
                    relatedType: "project",
                    relatedId: projectId,
                    projectId: projectId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được cảnh báo crisis cho project {ProjectId}", projectId);
        }
    }
}
