using System.Globalization;
using System.Text;
using System.Text.Json;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class BespokeReportService
{
    private readonly McfhDbContext _context;
    private readonly ProjectAnalyticsService _analytics;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public BespokeReportService(McfhDbContext context, ProjectAnalyticsService analytics)
    {
        _context = context;
        _analytics = analytics;
    }

    public async Task<BespokeCenterDto?> GetBespokeCenterAsync(int workspaceId, int projectId, int userId)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null) return null;

        var requests = await LoadProjectRequestsAsync(projectId);
        var dto = new BespokeCenterDto
        {
            UserSystemRole = user.SystemRole,
            Requests = requests.Select(r => MapRequest(r, user)).ToList()
        };

        if (IsAdmin(user))
        {
            dto.Reporters = await _context.Users
                .Where(u => u.SystemRole == "Reporter" && u.IsBanned != true)
                .OrderBy(u => u.FullName)
                .Select(u => new ReporterOptionDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();
        }

        return dto;
    }

    public async Task<BespokeRequestItemDto?> CreateRequestAsync(
        int workspaceId, int projectId, int userId, CreateBespokeRequestDto dto)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null) return null;

        var meta = new BespokeMeta
        {
            ProjectId = projectId,
            WorkspaceId = workspaceId,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo,
            Modules = dto.Modules.Count > 0 ? dto.Modules : DefaultModules(),
            Format = dto.Format ?? "html"
        };

        var request = new BespokeRequest
        {
            ClientId = userId,
            Title = dto.Title.Trim(),
            Requirements = dto.Requirements?.Trim(),
            CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions),
            Status = "pending",
            Deadline = ParseDate(dto.DateTo)?.AddDays(7)
        };

        _context.BespokeRequests.Add(request);
        await _context.SaveChangesAsync();

        request.Client = user;
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> AssignReporterAsync(
        int workspaceId, int projectId, int adminUserId, int requestId, int reporterId)
    {
        var admin = await GetUserWithAccessAsync(workspaceId, projectId, adminUserId);
        if (admin == null || !IsAdmin(admin)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null) return null;

        var reporter = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == reporterId && u.SystemRole == "Reporter");
        if (reporter == null) return null;

        request.ReporterId = reporterId;
        request.AssignedBy = adminUserId;
        request.AssignedAt = DateTime.Now;
        request.Status = "assigned";
        await _context.SaveChangesAsync();

        await _context.Entry(request).Reference(r => r.Reporter).LoadAsync();
        await _context.Entry(request).Reference(r => r.Client).LoadAsync();
        return MapRequest(request, admin);
    }

    public async Task<BespokeRequestItemDto?> StartWorkAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || !CanWorkOnRequest(user, request)) return null;

        request.Status = "in_progress";
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> DeliverReportAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || !CanWorkOnRequest(user, request)) return null;

        var meta = ParseMeta(request.CustomMetrics);
        var html = await BuildDeepReportHtmlAsync(workspaceId, projectId, userId, request, meta);

        var folder = GetBespokeFolder(requestId);
        Directory.CreateDirectory(folder);
        var fileName = $"bespoke-{requestId}.html";
        var filePath = Path.Combine(folder, fileName);
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

        var relativePath = Path.Combine("StorageData", "bespoke", requestId.ToString(), fileName);
        var version = $"v{(request.BespokeReports.Count + 1):D2}";

        _context.BespokeReports.Add(new BespokeReport
        {
            RequestId = requestId,
            FileUrl = relativePath,
            Version = version,
            UploadedAt = DateTime.Now
        });

        request.Status = "completed";
        request.SubmittedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<(byte[] Content, string FileName)?> DownloadDeliverableAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null) return null;

        if (!IsAdmin(user) && !(IsReporter(user) && request.ReporterId == userId))
        {
            var member = await GetUserWithAccessAsync(workspaceId, projectId, userId);
            if (member == null) return null;
        }

        var report = await _context.BespokeReports
            .Where(r => r.RequestId == requestId)
            .OrderByDescending(r => r.UploadedAt)
            .FirstOrDefaultAsync();

        if (report == null) return null;

        var path = ResolveFilePath(report.FileUrl);
        if (!File.Exists(path)) return null;

        var bytes = await File.ReadAllBytesAsync(path);
        return (bytes, $"bespoke-report-{requestId}.html");
    }

    private async Task<string> BuildDeepReportHtmlAsync(
        int workspaceId, int projectId, int userId, BespokeRequest request, BespokeMeta meta)
    {
        var project = await _context.Projects.FindAsync(projectId);
        var modules = meta.Modules.Count > 0 ? meta.Modules : DefaultModules();

        var overview = modules.Contains("overview")
            ? await _analytics.GetOverviewAsync(workspaceId, projectId, userId) : null;
        var sentiment = modules.Contains("sentiment")
            ? await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId) : null;
        var channels = modules.Contains("channel")
            ? await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId) : null;
        var influencers = modules.Contains("influencers")
            ? await _analytics.GetInfluencersAsync(workspaceId, projectId, userId) : null;
        var aspects = modules.Contains("aspects")
            ? await _analytics.GetAspectAnalysisAsync(workspaceId, projectId, userId) : null;

        var mentions = modules.Contains("top_mentions")
            ? await _analytics.GetMentionsAsync(workspaceId, projectId, userId) : null;

        if (meta.DateFrom != null || meta.DateTo != null)
            mentions = FilterMentionsByDate(mentions, meta.DateFrom, meta.DateTo);

        var generated = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Báo cáo chuyên sâu — {Esc(project?.Name)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Georgia,'Segoe UI',serif;background:#fff;color:#111;margin:0;padding:0;line-height:1.6;}");
        sb.AppendLine(".cover{min-height:100vh;background:linear-gradient(135deg,#0A101D,#151B2B);color:#fff;display:flex;flex-direction:column;justify-content:center;padding:60px;page-break-after:always;}");
        sb.AppendLine(".cover h1{font-size:2.5rem;color:#FF7575;margin:0 0 12px;} .cover .sub{color:#94a3b8;font-size:1.1rem;}");
        sb.AppendLine(".section{padding:48px 60px;page-break-inside:avoid;} h2{color:#0A101D;border-bottom:3px solid #FF7575;padding-bottom:8px;}");
        sb.AppendLine(".exec{background:#f8fafc;border-left:4px solid #00B4D8;padding:20px 24px;margin:24px 0;border-radius:0 8px 8px 0;}");
        sb.AppendLine(".req{background:#fef3c7;border-left:4px solid #EAB308;padding:16px 20px;margin:16px 0;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin:16px 0;} th,td{border:1px solid #e5e7eb;padding:10px;text-align:left;font-size:14px;}");
        sb.AppendLine("th{background:#f3f4f6;} .meta{color:#6b7280;font-size:13px;}");
        sb.AppendLine("@media print{.cover{min-height:auto;padding:40px;}}</style></head><body>");

        sb.AppendLine("<div class=\"cover\">");
        sb.AppendLine("<p class=\"sub\">MCFH — Báo cáo chuyên sâu (Bespoke)</p>");
        sb.AppendLine($"<h1>{Esc(request.Title)}</h1>");
        sb.AppendLine($"<p class=\"sub\">Dự án: {Esc(project?.Name)}</p>");
        if (!string.IsNullOrWhiteSpace(meta.DateFrom) || !string.IsNullOrWhiteSpace(meta.DateTo))
            sb.AppendLine($"<p class=\"sub\">Giai đoạn: {Esc(meta.DateFrom ?? "—")} → {Esc(meta.DateTo ?? "—")}</p>");
        sb.AppendLine($"<p class=\"sub\">Hoàn thành: {generated}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"section\">");
        sb.AppendLine("<h2>Tóm tắt điều hành</h2>");
        sb.AppendLine("<div class=\"exec\">");
        sb.AppendLine(BuildExecutiveSummary(overview, sentiment, channels));
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(request.Requirements))
        {
            sb.AppendLine("<h2>Yêu cầu khách hàng</h2>");
            sb.AppendLine($"<div class=\"req\">{Esc(request.Requirements).Replace("\n", "<br/>")}</div>");
        }
        sb.AppendLine("</div>");

        if (overview != null)
        {
            sb.AppendLine("<div class=\"section\"><h2>Tổng quan dữ liệu</h2><table>");
            sb.AppendLine($"<tr><td>Tổng mentions</td><td><strong>{overview.TotalMentions}</strong></td></tr>");
            sb.AppendLine($"<tr><td>Tổng bình luận</td><td><strong>{overview.TotalComments}</strong></td></tr>");
            sb.AppendLine($"<tr><td>NSR Score</td><td><strong>{overview.NsrScore:+#.#;-#.#;0}%</strong></td></tr>");
            sb.AppendLine($"<tr><td>Đã phân tích AI</td><td>{overview.AnalyzedCount} / {overview.TotalMentions}</td></tr>");
            sb.AppendLine("</table></div>");
        }

        if (sentiment != null)
        {
            sb.AppendLine("<div class=\"section\"><h2>Phân tích cảm xúc</h2><table>");
            sb.AppendLine($"<tr><th>Loại</th><th>Số lượng</th><th>Tỷ lệ</th></tr>");
            sb.AppendLine($"<tr><td>Tích cực</td><td>{sentiment.Positive}</td><td>{sentiment.PositivePercent}%</td></tr>");
            sb.AppendLine($"<tr><td>Tiêu cực</td><td>{sentiment.Negative}</td><td>{sentiment.NegativePercent}%</td></tr>");
            sb.AppendLine($"<tr><td>Trung lập</td><td>{sentiment.Neutral}</td><td>{sentiment.NeutralPercent}%</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<p class=\"meta\">Đánh giá: Cộng đồng chủ yếu {(sentiment.NsrScore >= 0 ? "thái độ tích cực" : "có dấu hiệu tiêu cực")} (NSR {sentiment.NsrScore:+#.#;-#.#;0}%).</p></div>");
        }

        if (channels?.Channels.Count > 0)
        {
            sb.AppendLine("<div class=\"section\"><h2>So sánh kênh</h2><table><tr><th>Kênh</th><th>Mentions</th><th>SOV</th><th>NSR</th></tr>");
            foreach (var ch in channels.Channels)
                sb.AppendLine($"<tr><td>{Esc(ch.Label)}</td><td>{ch.Mentions}</td><td>{ch.MentionShare}%</td><td>{ch.NsrScore}%</td></tr>");
            sb.AppendLine("</table></div>");
        }

        if (influencers?.Influencers.Count > 0)
        {
            sb.AppendLine("<div class=\"section\"><h2>KOLs & Influencers</h2><table><tr><th>Tên</th><th>Nền tảng</th><th>SOV</th><th>Sentiment</th></tr>");
            foreach (var k in influencers.Influencers.Take(15))
                sb.AppendLine($"<tr><td>{Esc(k.Name)}</td><td>{Esc(k.Platform)}</td><td>{k.ShareOfVoice}%</td><td>{Esc(k.DominantSentiment)}</td></tr>");
            sb.AppendLine("</table></div>");
        }

        if (aspects?.Aspects.Count > 0)
        {
            sb.AppendLine("<div class=\"section\"><h2>Phân tích khía cạnh</h2><table><tr><th>Khía cạnh</th><th>Tích cực</th><th>Tiêu cực</th></tr>");
            foreach (var a in aspects.Aspects)
                sb.AppendLine($"<tr><td>{Esc(a.Label)}</td><td>{a.PositivePercent}%</td><td>{a.NegativePercent}%</td></tr>");
            sb.AppendLine("</table></div>");
        }

        if (mentions?.Count > 0)
        {
            sb.AppendLine("<div class=\"section\"><h2>Mentions tiêu biểu</h2><table><tr><th>Nền tảng</th><th>Nội dung</th><th>Sentiment</th></tr>");
            foreach (var m in mentions.Take(20))
            {
                var preview = m.Content.Length > 120 ? m.Content[..120] + "…" : m.Content;
                sb.AppendLine($"<tr><td>{Esc(m.Platform)}</td><td>{Esc(preview)}</td><td>{Esc(m.Sentiment)}</td></tr>");
            }
            sb.AppendLine("</table></div>");
        }

        sb.AppendLine("<div class=\"section\"><h2>Khuyến nghị</h2><ul>");
        sb.AppendLine(BuildRecommendations(sentiment, channels, aspects));
        sb.AppendLine("</ul><p class=\"meta\">Báo cáo do Reporter MCFH biên soạn dựa trên dữ liệu scraping + AI sentiment.</p></div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildExecutiveSummary(
        ProjectOverviewDto? overview, SentimentSummaryDto? sentiment, ChannelComparisonDto? channels)
    {
        if (overview == null) return "Chưa có đủ dữ liệu tổng quan cho giai đoạn yêu cầu.";
        var topChannel = channels?.Channels.OrderByDescending(c => c.Mentions).FirstOrDefault();
        var parts = new List<string>
        {
            $"Trong phạm vi phân tích, hệ thống ghi nhận {overview.TotalMentions} mentions và {overview.TotalComments} bình luận."
        };
        if (sentiment != null)
            parts.Add($"Thái độ cộng đồng có NSR {sentiment.NsrScore:+#.#;-#.#;0}% — {sentiment.PositivePercent}% tích cực, {sentiment.NegativePercent}% tiêu cực.");
        if (topChannel != null)
            parts.Add($"Kênh dẫn đầu là {topChannel.Label} ({topChannel.MentionShare}% SOV).");
        return string.Join(" ", parts);
    }

    private static string BuildRecommendations(
        SentimentSummaryDto? sentiment, ChannelComparisonDto? channels, AspectAnalysisDto? aspects)
    {
        var items = new List<string>();
        if (sentiment?.NegativePercent > 30)
            items.Add("<li>Theo dõi sát mentions tiêu cực và chuẩn bị kịch bản phản hồi truyền thông.</li>");
        if (channels?.Channels.Any(c => c.NsrScore < -10) == true)
            items.Add("<li>Ưu tiên cải thiện nội dung trên kênh có NSR thấp.</li>");
        var topNegAspect = aspects?.Aspects.OrderByDescending(a => a.NegativePercent).FirstOrDefault();
        if (topNegAspect?.NegativePercent > 20)
            items.Add($"<li>Chủ đề «{Esc(topNegAspect.Label)}» đang bị phàn nàn — cần hành động khắc phục.</li>");
        if (items.Count == 0)
            items.Add("<li>Duy trì theo dõi định kỳ và cập nhật báo cáo khi có biến động sentiment.</li>");
        return string.Join("", items);
    }

    private static List<MentionDto>? FilterMentionsByDate(List<MentionDto>? mentions, string? from, string? to)
    {
        if (mentions == null) return null;
        var fromDt = ParseDate(from);
        var toDt = ParseDate(to)?.AddDays(1);

        return mentions.Where(m =>
        {
            if (m.ScrapedAt == null) return true;
            if (fromDt.HasValue && m.ScrapedAt < fromDt) return false;
            if (toDt.HasValue && m.ScrapedAt >= toDt) return false;
            return true;
        }).ToList();
    }

    private async Task<List<BespokeRequest>> LoadProjectRequestsAsync(int projectId)
    {
        var all = await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .OrderByDescending(r => r.RequestId)
            .ToListAsync();

        return all.Where(r => ParseMeta(r.CustomMetrics).ProjectId == projectId).ToList();
    }

    private async Task<BespokeRequest?> GetProjectRequestAsync(int projectId, int requestId)
    {
        var request = await _context.BespokeRequests
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null) return null;
        return ParseMeta(request.CustomMetrics).ProjectId == projectId ? request : null;
    }

    private async Task LoadRequestNavigationsAsync(BespokeRequest request)
    {
        await _context.Entry(request).Reference(r => r.Client).LoadAsync();
        await _context.Entry(request).Reference(r => r.Reporter).LoadAsync();
        await _context.Entry(request).Collection(r => r.BespokeReports).LoadAsync();
    }

    private BespokeRequestItemDto MapRequest(BespokeRequest r, User currentUser)
    {
        var meta = ParseMeta(r.CustomMetrics);
        var latestReport = r.BespokeReports.OrderByDescending(b => b.UploadedAt).FirstOrDefault();

        return new BespokeRequestItemDto
        {
            RequestId = r.RequestId,
            Title = r.Title,
            Requirements = r.Requirements,
            Status = r.Status ?? "pending",
            StatusLabel = StatusLabel(r.Status),
            Deadline = r.Deadline,
            SubmittedAt = r.SubmittedAt,
            AssignedAt = r.AssignedAt,
            ClientName = r.Client?.FullName,
            ReporterName = r.Reporter?.FullName,
            ReporterId = r.ReporterId,
            Modules = meta.Modules,
            DateFrom = meta.DateFrom,
            DateTo = meta.DateTo,
            Format = meta.Format,
            HasDeliverable = latestReport != null,
            DeliverableReportId = latestReport?.ReportId
        };
    }

    private static bool CanWorkOnRequest(User user, BespokeRequest request) =>
        IsAdmin(user) || (IsReporter(user) && request.ReporterId == user.UserId);

    private static bool IsAdmin(User u) =>
        u.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsReporter(User u) =>
        u.SystemRole.Equals("Reporter", StringComparison.OrdinalIgnoreCase);

    private async Task<User?> GetUserWithAccessAsync(int workspaceId, int projectId, int userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return null;

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId && p.WorkspaceId == workspaceId && p.IsDeleted != true);
        if (!projectExists) return null;

        return await _context.Users.FindAsync(userId);
    }

    private static BespokeMeta ParseMeta(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new BespokeMeta();
        try
        {
            return JsonSerializer.Deserialize<BespokeMeta>(json, JsonOptions) ?? new BespokeMeta();
        }
        catch
        {
            return new BespokeMeta();
        }
    }

    private static string StatusLabel(string? status) => status?.ToLowerInvariant() switch
    {
        "assigned" => "Đã giao Reporter",
        "quoted" => "Đã báo giá",
        "in_progress" => "Reporter đang làm",
        "completed" => "Hoàn thành",
        "cancelled" => "Đã hủy",
        _ => "Chờ xử lý"
    };

    private static List<string> DefaultModules() =>
        ["overview", "sentiment", "channel", "influencers", "aspects"];

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static string GetBespokeFolder(int requestId) =>
        Path.Combine(AppContext.BaseDirectory, "StorageData", "bespoke", requestId.ToString());

    private static string ResolveFilePath(string stored)
    {
        if (Path.IsPathRooted(stored) && File.Exists(stored)) return stored;
        var relative = Path.Combine(AppContext.BaseDirectory, stored);
        return File.Exists(relative) ? relative : stored;
    }

    private static string Esc(string? t) =>
        string.IsNullOrEmpty(t) ? "" : t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ——— Portal Admin / Reporter (không yêu cầu workspace member) ———

    public async Task<List<PortalBespokeRequestDto>> ListPortalRequestsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new();

        var requests = await LoadAllRequestsWithNavigationsAsync();
        if (IsAdmin(user))
            return await MapPortalListAsync(requests);

        if (!IsReporter(user)) return new();

        var mine = requests.Where(r =>
            r.ReporterId == userId ||
            (r.Status == "pending" && r.ReporterId == null)).ToList();
        return await MapPortalListAsync(mine);
    }

    public async Task<PortalBespokeRequestDto?> GetPortalRequestAsync(int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        if (!CanViewPortalRequest(user, request)) return null;
        return await MapPortalRequestAsync(request);
    }

    public async Task<BespokeRequestItemDto?> AssignReporterGlobalAsync(
        int adminUserId, int requestId, int reporterId)
    {
        var admin = await _context.Users.FindAsync(adminUserId);
        if (admin == null || !IsAdmin(admin)) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        var reporter = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == reporterId && u.SystemRole == "Reporter");
        if (reporter == null) return null;

        request.ReporterId = reporterId;
        request.AssignedBy = adminUserId;
        request.AssignedAt = DateTime.Now;
        request.Status = "assigned";
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, admin);
    }

    public async Task<BespokeRequestItemDto?> QuoteRequestAsync(
        int userId, int requestId, QuoteBespokeDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !IsReporter(user)) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        var status = request.Status?.ToLowerInvariant();
        if (status is not ("pending" or "assigned" or "quoted")) return null;
        if (request.ReporterId.HasValue && request.ReporterId != userId) return null;

        request.AgreedPrice = dto.AgreedPrice;
        if (dto.Deadline.HasValue) request.Deadline = dto.Deadline;
        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            request.Requirements = string.IsNullOrWhiteSpace(request.Requirements)
                ? $"[Báo giá Reporter]: {dto.Note.Trim()}"
                : $"{request.Requirements}\n\n[Báo giá Reporter]: {dto.Note.Trim()}";
        }

        request.Status = "quoted";
        if (!request.ReporterId.HasValue) request.ReporterId = userId;
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> StartWorkByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await StartWorkAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    public async Task<BespokeRequestItemDto?> DeliverReportByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await DeliverReportAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    public async Task<(byte[] Content, string FileName)?> DownloadByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await DownloadDeliverableAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    private async Task<(int WorkspaceId, int ProjectId)?> ResolveRequestContextAsync(int requestId)
    {
        var request = await _context.BespokeRequests.FindAsync(requestId);
        if (request == null) return null;
        var meta = ParseMeta(request.CustomMetrics);
        if (meta.ProjectId <= 0 || meta.WorkspaceId <= 0) return null;
        return (meta.WorkspaceId, meta.ProjectId);
    }

    private async Task<BespokeRequest?> GetRequestWithNavigationsAsync(int requestId) =>
        await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

    private async Task<List<BespokeRequest>> LoadAllRequestsWithNavigationsAsync() =>
        await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .OrderByDescending(r => r.RequestId)
            .ToListAsync();

    private static bool CanViewPortalRequest(User user, BespokeRequest request) =>
        IsAdmin(user) ||
        (IsReporter(user) && (request.ReporterId == user.UserId || request.Status == "pending"));

    private async Task<List<PortalBespokeRequestDto>> MapPortalListAsync(List<BespokeRequest> requests)
    {
        var result = new List<PortalBespokeRequestDto>();
        foreach (var r in requests)
            result.Add(await MapPortalRequestAsync(r));
        return result;
    }

    private async Task<PortalBespokeRequestDto> MapPortalRequestAsync(BespokeRequest r)
    {
        var meta = ParseMeta(r.CustomMetrics);
        var latestReport = r.BespokeReports.OrderByDescending(b => b.UploadedAt).FirstOrDefault();

        string? projectName = null;
        string? workspaceName = null;
        if (meta.ProjectId > 0)
        {
            var project = await _context.Projects.FindAsync(meta.ProjectId);
            projectName = project?.Name;
        }
        if (meta.WorkspaceId > 0)
        {
            var ws = await _context.Workspaces.FindAsync(meta.WorkspaceId);
            workspaceName = ws?.Name;
        }

        return new PortalBespokeRequestDto
        {
            RequestId = r.RequestId,
            Title = r.Title,
            Requirements = r.Requirements,
            Status = r.Status ?? "pending",
            StatusLabel = StatusLabel(r.Status),
            Deadline = r.Deadline,
            SubmittedAt = r.SubmittedAt,
            AssignedAt = r.AssignedAt,
            ClientName = r.Client?.FullName,
            ReporterName = r.Reporter?.FullName,
            ReporterId = r.ReporterId,
            WorkspaceId = meta.WorkspaceId,
            ProjectId = meta.ProjectId,
            ProjectName = projectName,
            WorkspaceName = workspaceName,
            Modules = meta.Modules,
            DateFrom = meta.DateFrom,
            DateTo = meta.DateTo,
            AgreedPrice = r.AgreedPrice,
            HasDeliverable = latestReport != null,
            DeliverableReportId = latestReport?.ReportId
        };
    }

    private sealed class BespokeMeta
    {
        public int ProjectId { get; set; }
        public int WorkspaceId { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public List<string> Modules { get; set; } = new();
        public string Format { get; set; } = "html";
    }
}
