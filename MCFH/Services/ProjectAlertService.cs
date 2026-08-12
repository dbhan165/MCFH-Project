using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class ProjectAlertService
{
    private readonly McfhDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProjectAlertService> _logger;

    public ProjectAlertService(
        McfhDbContext context,
        INotificationService notifications,
        IEmailService emailService,
        ILogger<ProjectAlertService> logger)
    {
        _context = context;
        _notifications = notifications;
        _emailService = emailService;
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

            var validFeedbacks = feedbacks.Where(f =>
            {
                var author = f.AuthorName?.Trim();
                var platform = (f.Platform ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(author) && mutedAuthors.Contains(author)) return false;
                if (!string.IsNullOrWhiteSpace(platform) && mutedPlatforms.Contains(platform)) return false;
                return true;
            }).ToList();

            var (positive, negative, neutral, unanalyzed, analyzed, nsr) = NsrCalculator.CalculateFromFeedbacks(validFeedbacks);
            var crisisTotal = validFeedbacks.Count(f => f.AiAnalysis?.IsCrisisAlert == true);

            if (analyzed == 0 && crisisCountInBatch == 0) return;

            var negativePercent = analyzed > 0 ? negative * 100.0 / analyzed : 0;
            var negativeTrend = nsr < 0 || negativePercent >= 35;
            var shouldAlert = crisisCountInBatch > 0 || crisisTotal > 0 || negativeTrend;
            if (!shouldAlert) return;

            var members = await _context.WorkspaceMembers
                .Where(m => m.WorkspaceId == project.WorkspaceId)
                .Select(m => m.User)
                .Distinct()
                .ToListAsync();

            if (members.Count == 0) return;

            var since = DateTime.Now.AddHours(-6);
            var title = crisisCountInBatch > 0 || crisisTotal > 0
                ? "Cảnh báo khủng hoảng truyền thông"
                : "Xu hướng tiêu cực được phát hiện";

            var message = crisisCountInBatch > 0 || crisisTotal > 0
                ? $"Dự án «{project.Name}»: phát hiện {Math.Max(crisisCountInBatch, crisisTotal)} mention khủng hoảng. NSR {nsr:+#0.#;-#0.#;0}%, tiêu cực {negativePercent:0.#}%."
                : $"Dự án «{project.Name}»: NSR {nsr:+#0.#;-#0.#;0}% với {negativePercent:0.#}% mention tiêu cực. Nên xem tab Sentiment.";

            foreach (var user in members)
            {
                if (user == null) continue;

                var alreadySent = await _context.Notifications.AnyAsync(n =>
                    n.UserId == user.UserId &&
                    n.ProjectId == projectId &&
                    n.Type == "crisis_alert" &&
                    n.CreatedAt >= since);

                if (alreadySent) continue;

                await _notifications.NotifyAsync(
                    user.UserId,
                    title,
                    message,
                    "crisis_alert",
                    relatedType: "project",
                    relatedId: projectId,
                    projectId: projectId);

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Inter', 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 40px 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.05); border: 1px solid #fee2e2; }}
        .header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); padding: 30px 40px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }}
        .alert-icon {{ font-size: 48px; margin-bottom: 10px; display: block; }}
        .content {{ padding: 40px; }}
        .title {{ color: #1e293b; font-size: 20px; font-weight: 600; margin-top: 0; margin-bottom: 15px; }}
        .message-box {{ background: #fef2f2; border-left: 4px solid #ef4444; padding: 15px 20px; border-radius: 4px; color: #7f1d1d; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
        .btn {{ display: inline-block; background: #ef4444; color: #ffffff; text-decoration: none; padding: 14px 28px; border-radius: 8px; font-weight: 600; font-size: 16px; transition: background 0.2s; }}
        .btn:hover {{ background: #dc2626; }}
        .footer {{ padding: 20px 40px; background: #f8fafc; border-top: 1px solid #f1f5f9; text-align: center; color: #64748b; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='alert-icon'>🚨</span>
            <h1>Cảnh Báo Khẩn Cấp</h1>
        </div>
        <div class='content'>
            <h2 class='title'>{title}</h2>
            <div class='message-box'>
                {message}
            </div>
            <p style='color: #475569; font-size: 15px; line-height: 1.6; margin-bottom: 30px;'>
                Hệ thống AI của chúng tôi vừa phát hiện hoạt động bất thường hoặc xu hướng tiêu cực tăng đột biến liên quan đến dự án của bạn. Vui lòng kiểm tra ngay để có phương án xử lý kịp thời.
            </p>
            <div style='text-align: center;'>
                <a href='http://localhost:5173/admin' class='btn'>Truy cập hệ thống MCFH ngay</a>
            </div>
        </div>
        <div class='footer'>
            Email này được gửi tự động từ Hệ thống Giám sát MCFH (AI Sentiment).<br>
            Vui lòng không trả lời trực tiếp email này.
        </div>
    </div>
</body>
</html>";
                    await _emailService.SendEmailAsync(user.Email, title, htmlMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được cảnh báo crisis cho project {ProjectId}", projectId);
        }
    }
}
