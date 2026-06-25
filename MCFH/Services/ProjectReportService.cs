using System.Globalization;
using System.Text;
using System.Text.Json;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace MCFH.Services;

public class ProjectReportService
{
    private readonly McfhDbContext _context;
    private readonly ProjectAnalyticsService _analytics;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly List<ReportTemplateDto> Templates =
    [
        new()
        {
            Key = "mentions-csv",
            Name = "Xuất Mentions (CSV)",
            Description = "Toàn bộ mentions: nền tảng, tác giả, nội dung, sentiment, link.",
            Format = "csv",
            TypeLabel = "CSV Raw Data"
        },
        new()
        {
            Key = "analytics-pdf",
            Name = "Báo cáo Tổng quan (PDF)",
            Description = "Xuất báo cáo analytics dạng PDF chuẩn in ấn.",
            Format = "pdf",
            TypeLabel = "PDF Report"
        },
        new()
        {
            Key = "analytics-html",
            Name = "Báo cáo Tổng quan (HTML)",
            Description = "Tổng hợp overview, sentiment, kênh, KOL — mở bằng trình duyệt hoặc in PDF.",
            Format = "html",
            TypeLabel = "HTML Report"
        },
        new()
        {
            Key = "analytics-json",
            Name = "Dữ liệu Analytics (JSON)",
            Description = "Gói JSON đầy đủ: overview, sentiment, channels, influencers, aspects.",
            Format = "json",
            TypeLabel = "JSON Export"
        }
    ];

    public ProjectReportService(McfhDbContext context, ProjectAnalyticsService analytics)
    {
        _context = context;
        _analytics = analytics;
    }

    public async Task<ReportCenterDto?> GetReportCenterAsync(int workspaceId, int projectId, int userId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;

        var reports = await LoadIndexAsync(projectId);
        return new ReportCenterDto
        {
            TotalReports = reports.Count,
            LastGeneratedAt = reports.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.CreatedAt,
            Templates = Templates,
            Reports = reports.OrderByDescending(r => r.CreatedAt).ToList()
        };
    }

    public async Task<ReportFileDto?> GenerateReportAsync(
        int workspaceId, int projectId, int userId, string type, string authorName)
    {
        var project = await GetProjectAsync(workspaceId, projectId, userId);
        if (project == null) return null;

        var template = Templates.FirstOrDefault(t => t.Key == type);
        if (template == null) return null;

        var reportId = $"REP-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
        var folder = GetReportFolder(projectId);
        Directory.CreateDirectory(folder);

        string extension;
        int rowCount;
        var fileName = $"{SanitizeFileName(template.Name)}-{reportId}";

        if (type == "analytics-pdf")
        {
            var (pdfBytes, ext, count) = await BuildAnalyticsPdfAsync(workspaceId, projectId, userId, project.Name);
            extension = ext;
            rowCount = count;
            var filePath = Path.Combine(folder, $"{fileName}.{extension}");
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            fileName = $"{fileName}.{extension}";
        }
        else
        {
            (string content, extension, rowCount) = type switch
            {
                "mentions-csv" => await BuildMentionsCsvAsync(projectId, project.Name),
                "analytics-html" => await BuildAnalyticsHtmlAsync(workspaceId, projectId, userId, project.Name),
                "analytics-json" => await BuildAnalyticsJsonAsync(workspaceId, projectId, userId, project.Name),
                _ => throw new ArgumentException("Loại báo cáo không hợp lệ.")
            };
            var filePath = Path.Combine(folder, $"{fileName}.{extension}");
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            fileName = $"{fileName}.{extension}";
        }

        var savedPath = Path.Combine(folder, fileName);
        var fileInfo = new FileInfo(savedPath);
        var entry = new ReportFileDto
        {
            ReportId = reportId,
            Name = $"{template.Name} — {project.Name}",
            Type = type,
            TypeLabel = template.TypeLabel,
            CreatedAt = DateTime.Now,
            CreatedBy = string.IsNullOrWhiteSpace(authorName) ? "Hệ thống" : authorName,
            Status = "ready",
            FileSizeBytes = fileInfo.Length,
            RowCount = rowCount
        };

        var index = await LoadIndexAsync(projectId);
        index.Insert(0, entry);
        await SaveIndexAsync(projectId, index, fileName);

        return entry;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadReportAsync(
        int workspaceId, int projectId, int userId, string reportId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;

        var reports = await LoadIndexAsync(projectId);
        var meta = reports.FirstOrDefault(r => r.ReportId == reportId);
        if (meta == null) return null;

        var filePath = ResolveReportPath(projectId, reportId);
        if (!File.Exists(filePath)) return null;

        var bytes = await File.ReadAllBytesAsync(filePath);
        var contentType = meta.Type switch
        {
            "mentions-csv" => "text/csv; charset=utf-8",
            "analytics-html" => "text/html; charset=utf-8",
            "analytics-pdf" => "application/pdf",
            "analytics-json" => "application/json; charset=utf-8",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(filePath);
        return (bytes, contentType, fileName);
    }

    private async Task<(string Content, string Extension, int RowCount)> BuildMentionsCsvAsync(
        int projectId, string projectName)
    {
        var mentions = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .OrderByDescending(f => f.ScrapedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("feedback_id,platform,author,content,sentiment,comments_count,original_url,scraped_at");

        foreach (var m in mentions)
        {
            sb.AppendLine(string.Join(",",
                m.FeedbackId,
                Csv(m.Platform),
                Csv(m.AuthorName),
                Csv(m.Content),
                Csv(m.AiAnalysis?.MainSentiment),
                m.CommentsCount ?? 0,
                Csv(m.OriginalUrl),
                Csv(m.ScrapedAt?.ToString("o"))));
        }

        return (sb.ToString(), "csv", mentions.Count);
    }

    private async Task<(string Content, string Extension, int RowCount)> BuildAnalyticsHtmlAsync(
        int workspaceId, int projectId, int userId, string projectName)
    {
        var overview = await _analytics.GetOverviewAsync(workspaceId, projectId, userId);
        var sentiment = await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId);
        var channels = await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId);
        var influencers = await _analytics.GetInfluencersAsync(workspaceId, projectId, userId);
        var aspects = await _analytics.GetAspectAnalysisAsync(workspaceId, projectId, userId);

        var generated = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Báo cáo — {EscapeHtml(projectName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,system-ui,sans-serif;background:#0A101D;color:#e5e7eb;margin:0;padding:32px;}");
        sb.AppendLine("h1{color:#FF7575;} h2{color:#00B4D8;margin-top:32px;border-bottom:1px solid #333;padding-bottom:8px;}");
        sb.AppendLine(".card{background:#151B2B;border:1px solid #333;border-radius:12px;padding:16px;margin:12px 0;}");
        sb.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;}");
        sb.AppendLine(".stat{font-size:28px;font-weight:bold;color:#fff;} .label{font-size:12px;color:#9ca3af;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:12px;} th,td{padding:8px 12px;border-bottom:1px solid #333;text-align:left;font-size:13px;}");
        sb.AppendLine("th{color:#9ca3af;font-size:11px;text-transform:uppercase;}");
        sb.AppendLine("@media print{body{background:#fff;color:#111;} .card{border-color:#ddd;}}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>MCFH — Báo cáo Social Listening</h1>");
        sb.AppendLine($"<p>Dự án: <strong>{EscapeHtml(projectName)}</strong> · Tạo lúc: {generated}</p>");

        if (overview != null)
        {
            sb.AppendLine("<h2>Tổng quan</h2><div class=\"grid\">");
            AppendStat(sb, "Mentions", overview.TotalMentions);
            AppendStat(sb, "Bình luận", overview.TotalComments);
            AppendStat(sb, "NSR Score", $"{overview.NsrScore:+#.#;-#.#;0}%");
            AppendStat(sb, "Đã phân tích AI", overview.AnalyzedCount);
            sb.AppendLine("</div>");
        }

        if (sentiment != null)
        {
            sb.AppendLine("<h2>Sentiment</h2><div class=\"grid\">");
            AppendStat(sb, "Tích cực", $"{sentiment.Positive} ({sentiment.PositivePercent}%)");
            AppendStat(sb, "Tiêu cực", $"{sentiment.Negative} ({sentiment.NegativePercent}%)");
            AppendStat(sb, "Trung lập", $"{sentiment.Neutral} ({sentiment.NeutralPercent}%)");
            sb.AppendLine("</div>");
        }

        if (channels?.Channels.Count > 0)
        {
            sb.AppendLine("<h2>So sánh kênh</h2><table><tr><th>Nền tảng</th><th>Mentions</th><th>%</th><th>Comments</th><th>NSR</th></tr>");
            foreach (var ch in channels.Channels)
            {
                sb.AppendLine($"<tr><td>{EscapeHtml(ch.Label)}</td><td>{ch.Mentions}</td><td>{ch.MentionShare}%</td><td>{ch.TotalComments}</td><td>{ch.NsrScore}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        if (influencers?.Influencers.Count > 0)
        {
            sb.AppendLine("<h2>Top KOLs</h2><table><tr><th>Tên</th><th>Nền tảng</th><th>SOV</th><th>Mentions</th></tr>");
            foreach (var kol in influencers.Influencers.Take(10))
            {
                sb.AppendLine($"<tr><td>{EscapeHtml(kol.Name)}</td><td>{EscapeHtml(kol.Platform)}</td><td>{kol.ShareOfVoice}%</td><td>{kol.Mentions}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        if (aspects?.Aspects.Count > 0)
        {
            sb.AppendLine("<h2>Khía cạnh</h2><table><tr><th>Khía cạnh</th><th>Tích cực</th><th>Tiêu cực</th><th>Trung lập</th></tr>");
            foreach (var a in aspects.Aspects)
            {
                sb.AppendLine($"<tr><td>{EscapeHtml(a.Label)}</td><td>{a.PositivePercent}%</td><td>{a.NegativePercent}%</td><td>{a.NeutralPercent}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<p style=\"margin-top:48px;color:#6b7280;font-size:12px;\">Generated by MCFH Platform</p>");
        sb.AppendLine("</body></html>");

        return (sb.ToString(), "html", 1);
    }

    private async Task<(byte[] Content, string Extension, int RowCount)> BuildAnalyticsPdfAsync(
        int workspaceId, int projectId, int userId, string projectName)
    {
        var (html, _, rowCount) = await BuildAnalyticsHtmlAsync(workspaceId, projectId, userId, projectName);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin { Top = "20mm", Bottom = "20mm", Left = "15mm", Right = "15mm" }
        });
        return (pdfBytes, "pdf", rowCount);
    }

    private async Task<(string Content, string Extension, int RowCount)> BuildAnalyticsJsonAsync(
        int workspaceId, int projectId, int userId, string projectName)
    {
        var payload = new
        {
            projectName,
            generatedAt = DateTime.Now,
            overview = await _analytics.GetOverviewAsync(workspaceId, projectId, userId),
            sentiment = await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId),
            channels = await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId),
            influencers = await _analytics.GetInfluencersAsync(workspaceId, projectId, userId),
            aspects = await _analytics.GetAspectAnalysisAsync(workspaceId, projectId, userId)
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return (json, "json", 1);
    }

    private static void AppendStat(StringBuilder sb, string label, object value)
    {
        sb.AppendLine($"<div class=\"card\"><div class=\"label\">{EscapeHtml(label)}</div><div class=\"stat\">{EscapeHtml(value?.ToString() ?? "—")}</div></div>");
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string EscapeHtml(string? text) =>
        string.IsNullOrEmpty(text)
            ? ""
            : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Replace(" ", "-");

    private static string GetReportFolder(int projectId) =>
        Path.Combine(AppContext.BaseDirectory, "StorageData", "reports", projectId.ToString());

    private static string GetIndexPath(int projectId) =>
        Path.Combine(GetReportFolder(projectId), "index.json");

    private static string ResolveReportPath(int projectId, string reportId)
    {
        var folder = GetReportFolder(projectId);
        return Directory.GetFiles(folder, $"*{reportId}.*").FirstOrDefault()
               ?? Path.Combine(folder, $"{reportId}");
    }

    private async Task<List<ReportFileDto>> LoadIndexAsync(int projectId)
    {
        var path = GetIndexPath(projectId);
        if (!File.Exists(path)) return new List<ReportFileDto>();

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<ReportFileDto>>(json, JsonOptions) ?? new List<ReportFileDto>();
        }
        catch
        {
            return new List<ReportFileDto>();
        }
    }

    private async Task SaveIndexAsync(int projectId, List<ReportFileDto> index, string _)
    {
        Directory.CreateDirectory(GetReportFolder(projectId));
        await File.WriteAllTextAsync(GetIndexPath(projectId), JsonSerializer.Serialize(index, JsonOptions));
    }

    private async Task<bool> CanAccessProjectAsync(int workspaceId, int projectId, int userId)
    {
        return await GetProjectAsync(workspaceId, projectId, userId) != null;
    }

    private async Task<Project?> GetProjectAsync(int workspaceId, int projectId, int userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return null;

        return await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.WorkspaceId == workspaceId && p.IsDeleted != true);
    }
}
