using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace MCFH.Services;

public class ProjectReportService
{
    private readonly McfhDbContext _context;
    private readonly ProjectAnalyticsService _analytics;
    private readonly IAiSentimentService _aiSentiment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly List<ReportTemplateDto> Templates =
    [
        new()
        {
            Key = "analytics-pdf",
            Name = "Báo cáo Điều hành (PDF)",
            Description = "Bản báo cáo đẹp, chi tiết và sẵn sàng gửi khách hàng hoặc in ấn.",
            Format = "pdf",
            TypeLabel = "PDF Report"
        },
        new()
        {
            Key = "analytics-html",
            Name = "Báo cáo Chi tiết (HTML)",
            Description = "Bản xem chi tiết với KPI, insight, kênh, influencer, aspect và top mentions nổi bật.",
            Format = "html",
            TypeLabel = "HTML Report"
        },
        new()
        {
            Key = "mentions-xlsx",
            Name = "Xuất Mentions (Excel)",
            Description = "Danh sách mentions dạng bảng để lọc, tìm kiếm hoặc báo cáo (đã xử lý màu sắc).",
            Format = "xlsx",
            TypeLabel = "Excel Export"
        },
        new()
        {
            Key = "analytics-pptx",
            Name = "Báo cáo Trình chiếu (PPTX)",
            Description = "Slide tóm tắt KPI và insight để trình bày nhanh với team hoặc khách hàng.",
            Format = "pptx",
            TypeLabel = "PPTX Report"
        }
    ];

    public ProjectReportService(McfhDbContext context, ProjectAnalyticsService analytics, IAiSentimentService aiSentiment)
    {
        _context = context;
        _analytics = analytics;
        _aiSentiment = aiSentiment;
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
        int workspaceId, int projectId, int userId, string type, string authorName,
        string? displayName = null)
    {
        var project = await GetProjectAsync(workspaceId, projectId, userId);
        if (project == null) return null;

        var template = Templates.FirstOrDefault(t => t.Key == type);
        if (template == null) return null;

        var titleName = string.IsNullOrWhiteSpace(displayName) ? project.Name : displayName.Trim();

        var reportId = $"REP-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
        var folder = GetReportFolder(projectId);
        Directory.CreateDirectory(folder);

        string extension;
        int rowCount;
        var fileName = $"{SanitizeFileName(template.Name)}-{reportId}";

        if (type == "analytics-pdf")
        {
            var (pdfBytes, ext, count) = await BuildAnalyticsPdfAsync(workspaceId, projectId, userId, titleName);
            extension = ext;
            rowCount = count;
            var filePath = Path.Combine(folder, $"{fileName}.{extension}");
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            fileName = $"{fileName}.{extension}";
        }
        else if (type == "analytics-pptx" || type == "mentions-xlsx")
        {
            byte[] bytes;
            int count;
            if (type == "analytics-pptx")
            {
                (bytes, extension, count) = await BuildAnalyticsPptxAsync(workspaceId, projectId, userId, titleName);
            }
            else
            {
                (bytes, extension, count) = await BuildMentionsXlsxAsync(workspaceId, projectId, userId, titleName);
            }
            rowCount = count;
            var filePath = Path.Combine(folder, $"{fileName}.{extension}");
            await File.WriteAllBytesAsync(filePath, bytes);
            fileName = $"{fileName}.{extension}";
        }
        else
        {
            (string content, extension, rowCount) = type switch
            {
                "analytics-html" => await BuildAnalyticsHtmlAsync(workspaceId, projectId, userId, titleName),
                "analytics-json" => await BuildAnalyticsJsonAsync(workspaceId, projectId, userId, titleName),
                _ => throw new ArgumentException("Loại báo cáo không hợp lệ.")
            };
            var filePath = Path.Combine(folder, $"{fileName}.{extension}");
            await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(true));
            fileName = $"{fileName}.{extension}";
        }

        var savedPath = Path.Combine(folder, fileName);
        var fileInfo = new FileInfo(savedPath);
        var entry = new ReportFileDto
        {
            ReportId = reportId,
            Name = $"{template.Name} — {titleName}",
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

    /// <summary>
    /// Render PDF analytics in-memory — không ghi vào thư mục/index Reports của project
    /// (dùng cho bespoke để không làm bẩn danh sách báo cáo chung).
    /// Khi <paramref name="filter"/> được truyền vào, toàn bộ số liệu (overview, sentiment, kênh, influencer)
    /// được tính lại CHỈ từ tập mentions khớp filter — tránh trộn dữ liệu cũ của project vào báo cáo bespoke.
    /// </summary>
    public async Task<(byte[] Content, string FileName)?> RenderAnalyticsPdfAsync(
        int workspaceId, int projectId, int userId, string? displayName = null,
        MentionQueryDto? filter = null)
    {
        var project = await GetProjectAsync(workspaceId, projectId, userId);
        if (project == null) return null;

        var titleName = string.IsNullOrWhiteSpace(displayName) ? project.Name : displayName.Trim();
        try
        {
            var (pdfBytes, _, _) = await BuildAnalyticsPdfAsync(workspaceId, projectId, userId, titleName, filter);
            var fileName = $"{SanitizeFileName($"Bao-cao-{titleName}")}.pdf";
            return (pdfBytes, fileName);
        }
        catch (PlaywrightException ex)
        {
            // Thiếu Chromium / PLAYWRIGHT_BROWSERS_PATH sai — không để 500 trắng.
            throw new InvalidOperationException(
                "Không xuất được PDF: Playwright Chromium chưa được cài. Chạy: pwsh bin/Debug/net8.0/playwright.ps1 install chromium",
                ex);
        }
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
            "mentions-xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "analytics-html" => "text/html; charset=utf-8",
            "analytics-pdf" => "application/pdf",
            "analytics-pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "analytics-json" => "application/json; charset=utf-8",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(filePath);
        return (bytes, contentType, fileName);
    }

    private async Task<(byte[] Content, string Extension, int RowCount)> BuildMentionsXlsxAsync(
        int workspaceId, int projectId, int userId, string projectName)
    {
        var mentions = await _analytics.GetMentionsAsync(workspaceId, projectId, userId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mentions");

        // Headers
        var headers = new[] { "Feedback ID", "Nền tảng", "Tác giả", "Nội dung", "Sentiment", "Bình luận", "Ngày lấy dữ liệu", "AI Summary", "Original URL" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#D1D5DB");
        }
        ws.SheetView.FreezeRows(1);

        // Data
        int row = 2;
        foreach (var m in mentions)
        {
            ws.Cell(row, 1).Value = m.FeedbackId;
            ws.Cell(row, 2).Value = m.Platform ?? "";
            ws.Cell(row, 3).Value = m.AuthorName ?? "";
            ws.Cell(row, 4).Value = m.Content ?? "";
            ws.Cell(row, 4).Style.Alignment.WrapText = true;
            
            var sentiment = m.Sentiment ?? "";
            var senCell = ws.Cell(row, 5);
            senCell.Value = sentiment;
            if (sentiment.Equals("positive", StringComparison.OrdinalIgnoreCase))
            {
                senCell.Style.Font.FontColor = XLColor.FromHtml("#059669");
                senCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
            }
            else if (sentiment.Equals("negative", StringComparison.OrdinalIgnoreCase))
            {
                senCell.Style.Font.FontColor = XLColor.FromHtml("#E11D48");
                senCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE4E6");
            }
            else
            {
                senCell.Style.Font.FontColor = XLColor.FromHtml("#4B5563");
                senCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            }
            
            ws.Cell(row, 6).Value = m.CommentsCount;
            ws.Cell(row, 7).Value = m.ScrapedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(row, 8).Value = m.AiSummary ?? "";
            ws.Cell(row, 8).Style.Alignment.WrapText = true;
            ws.Cell(row, 9).Value = m.OriginalUrl ?? "";
            if (!string.IsNullOrEmpty(m.OriginalUrl))
            {
                ws.Cell(row, 9).SetHyperlink(new XLHyperlink(m.OriginalUrl));
            }
            
            row++;
        }

        ws.Column(1).Width = 15;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 60;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 10;
        ws.Column(7).Width = 18;
        ws.Column(8).Width = 40;
        ws.Column(9).Width = 25;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return (ms.ToArray(), "xlsx", mentions.Count);
    }

    private async Task<(string Content, string Extension, int RowCount)> BuildAnalyticsHtmlAsync(
        int workspaceId, int projectId, int userId, string projectName, MentionQueryDto? filter = null)
    {
        ProjectOverviewDto? overview;
        SentimentSummaryDto? sentiment;
        ChannelComparisonDto? channels;
        InfluencerAnalyticsDto? influencers;
        AspectAnalysisDto? aspects;
        List<MentionDto> mentions;

        if (filter != null)
        {
            // Chỉ tính toán từ tập mentions khớp filter (VD: theo keyword + mốc bắt đầu cào của đơn bespoke) —
            // KHÔNG gọi các API tổng hợp không filter, tránh trộn mentions cũ của project vào báo cáo.
            mentions = await _analytics.GetMentionsAsync(workspaceId, projectId, userId, filter);
            overview = BuildOverviewFromMentions(projectId, projectName, mentions);
            sentiment = BuildSentimentFromMentions(mentions);
            channels = BuildChannelsFromMentions(mentions);
            influencers = BuildInfluencersFromMentions(mentions);
            aspects = null; // bỏ qua khía cạnh khi lọc — tránh kéo dữ liệu aspect không lọc của cả project
        }
        else
        {
            overview = await _analytics.GetOverviewAsync(workspaceId, projectId, userId);
            sentiment = await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId);
            channels = await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId);
            influencers = await _analytics.GetInfluencersAsync(workspaceId, projectId, userId);
            aspects = await _analytics.GetAspectAnalysisAsync(workspaceId, projectId, userId);
            mentions = await _analytics.GetMentionsAsync(workspaceId, projectId, userId);
        }

        var generated = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
        var totalMentions = overview?.TotalMentions ?? mentions.Count;
        var totalComments = overview?.TotalComments ?? mentions.Sum(m => m.CommentsCount);
        var analyzedCount = overview?.AnalyzedCount ?? sentiment?.Total - sentiment?.Unanalyzed ?? mentions.Count(m => m.IsAnalyzed);
        var pendingCount = overview?.PendingAnalysisCount ?? sentiment?.Unanalyzed ?? mentions.Count(m => !m.IsAnalyzed);
        var coverage = totalMentions > 0 ? Math.Round(analyzedCount * 100.0 / totalMentions, 1) : 0;
        var dominantSentiment = ResolveDominantSentiment(sentiment);
        var topChannel = channels?.Channels.OrderByDescending(c => c.Mentions).FirstOrDefault();
        var topRiskChannel = channels?.Channels
            .Where(c => c.Positive + c.Negative + c.Neutral > 0)
            .OrderByDescending(c => c.NegativePercent)
            .FirstOrDefault();
        var topInfluencer = influencers?.Influencers
            .OrderByDescending(i => i.InfluenceScore)
            .ThenByDescending(i => i.Mentions)
            .FirstOrDefault();
        var nsrScore = overview?.NsrScore ?? sentiment?.NsrScore ?? 0;
        var topChannelInfo = topChannel != null ? $"{topChannel.Label} ({topChannel.MentionShare:0.#}% SOV, {topChannel.TotalComments} comments)" : "Không có dữ liệu";
        var topNegativeAspects = aspects != null && aspects.Aspects.Count > 0 
            ? string.Join(", ", aspects.Aspects.OrderByDescending(a => a.NegativePercent).Take(3).Select(a => $"{a.Label} ({a.NegativePercent:0.#}% tiêu cực)")) 
            : "Không có";

        var aiInsights = await _aiSentiment.GenerateReportInsightsAsync(
            projectName, totalMentions, nsrScore, topChannelInfo, topNegativeAspects);

        var executiveInsights = aiInsights?.ExecutiveInsights?.Count > 0 
            ? aiInsights.ExecutiveInsights 
            : BuildExecutiveInsights(
                totalMentions,
                totalComments,
                pendingCount,
                coverage,
                dominantSentiment,
                topChannel,
                topRiskChannel,
                topInfluencer,
                aspects);

        var actionItems = aiInsights?.ActionItems?.Count > 0 
            ? aiInsights.ActionItems 
            : BuildActionItems(pendingCount, topRiskChannel, topInfluencer, aspects);
        var mentionHighlights = mentions
            .OrderByDescending(m => m.PinnedForReport)
            .ThenByDescending(m => string.Equals(m.Sentiment, "negative", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.CommentsCount)
            .ThenByDescending(m => m.PostedAt ?? m.ScrapedAt ?? DateTime.MinValue)
            .Take(6)
            .ToList();
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Báo cáo — {EscapeHtml(projectName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(":root{color-scheme:light;--bg:#f8fafc;--panel:#ffffff;--text:#0f172a;--muted:#64748b;--line:#e2e8f0;--brand:#ef4444;--brand-2:#0ea5e9;--good:#10b981;--warn:#f59e0b;--bad:#ef4444;}");
        sb.AppendLine("*{box-sizing:border-box;} body{font-family:'Segoe UI',Inter,system-ui,sans-serif;background:var(--bg);color:var(--text);margin:0;padding:24px;-webkit-print-color-adjust:exact;print-color-adjust:exact;}");
        sb.AppendLine(".page{max-width:1180px;margin:0 auto;} .hero{background:linear-gradient(135deg,#0f172a 0%,#1e293b 100%);color:#fff;border-radius:24px;padding:36px;position:relative;overflow:hidden;box-shadow:0 20px 40px -15px rgba(0,0,0,0.15);}");
        sb.AppendLine(".hero:before,.hero:after{content:'';position:absolute;border-radius:999px;filter:blur(60px);opacity:.4;} .hero:before{width:250px;height:250px;background:var(--brand);top:-120px;right:-50px;} .hero:after{width:200px;height:200px;background:var(--brand-2);bottom:-100px;left:-40px;}");
        sb.AppendLine(".hero-inner{position:relative;z-index:1;} .eyebrow{display:inline-block;padding:6px 14px;border-radius:999px;background:rgba(255,255,255,.1);border:1px solid rgba(255,255,255,.15);font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#f8fafc;backdrop-filter:blur(4px);}");
        sb.AppendLine("h1{font-size:36px;font-weight:800;line-height:1.2;margin:18px 0 10px;} .hero p{margin:0;color:#cbd5e1;line-height:1.6;font-size:15px;max-width:800px;} .hero-meta{display:flex;flex-wrap:wrap;gap:24px;margin-top:24px;font-size:13px;color:#e2e8f0;background:rgba(0,0,0,0.2);padding:12px 18px;border-radius:12px;}");
        sb.AppendLine(".section{background:var(--panel);border:1px solid var(--line);border-radius:20px;padding:28px;margin-top:24px;box-shadow:0 8px 20px -8px rgba(15,23,42,.06);}");
        sb.AppendLine(".section h2{margin:0 0 10px;font-size:22px;font-weight:700;color:#1e293b;} .section p.sub{margin:0 0 20px;color:var(--muted);font-size:14px;line-height:1.6;}");
        sb.AppendLine(".stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:16px;margin-top:24px;} .stat-card{background:#f8fafc;border:1px solid var(--line);border-radius:16px;padding:20px;transition:transform 0.2s;} .stat-card:hover{transform:translateY(-2px);}");
        sb.AppendLine(".stat-label{font-size:12px;letter-spacing:.05em;text-transform:uppercase;color:var(--muted);font-weight:700;margin-bottom:10px;} .stat-value{font-size:30px;font-weight:800;color:var(--text);letter-spacing:-0.02em;} .stat-note{margin-top:10px;color:var(--muted);font-size:13px;line-height:1.5;}");
        sb.AppendLine(".hero .stat-card{background:rgba(255,255,255,0.06);border-color:rgba(255,255,255,0.12);} .hero .stat-value{color:#ffffff;} .hero .stat-label{color:#94a3b8;} .hero .stat-note{color:#cbd5e1;}");
        sb.AppendLine(".two-col{display:grid;grid-template-columns:1fr 1fr;gap:24px;} .stack{display:flex;flex-direction:column;gap:16px;} .insight-list,.action-list{margin:0;padding-left:20px;} .insight-list li,.action-list li{margin:0 0 14px;color:#334155;line-height:1.6;font-size:15px;}");
        sb.AppendLine(".callout{border-radius:16px;padding:20px;border:1px solid var(--line);} .callout.good{background:#ecfdf5;border-color:#a7f3d0;} .callout.warn{background:#fffbeb;border-color:#fde68a;} .callout.bad{background:#fef2f2;border-color:#fecaca;}");
        sb.AppendLine(".callout-title{font-weight:700;font-size:15px;margin-bottom:10px;display:flex;align-items:center;gap:8px;} .callout.good .callout-title{color:#059669;} .callout.bad .callout-title{color:#dc2626;} .callout.warn .callout-title{color:#d97706;} .callout-body{color:#334155;line-height:1.6;font-size:14px;}");
        sb.AppendLine("table{width:100%;border-collapse:separate;border-spacing:0;} th,td{padding:14px 12px;border-bottom:1px solid var(--line);text-align:left;vertical-align:top;font-size:14px;color:#334155;} th{font-size:12px;text-transform:uppercase;letter-spacing:.05em;color:var(--muted);font-weight:600;background:#f8fafc;} tr:last-child td{border-bottom:none;} tbody tr:hover{background:#f8fafc;}");
        sb.AppendLine(".num{font-variant-numeric:tabular-nums;font-weight:500;} .pill{display:inline-flex;align-items:center;padding:4px 12px;border-radius:999px;font-size:12px;font-weight:600;} .pill.positive{background:#d1fae5;color:#047857;} .pill.negative{background:#fee2e2;color:#b91c1c;} .pill.neutral{background:#fef3c7;color:#b45309;} .pill.pending{background:#f1f5f9;color:#475569;}");
        sb.AppendLine(".mentions{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px;} .mention{border:1px solid var(--line);border-radius:16px;padding:20px;background:#fff;box-shadow:0 4px 12px -8px rgba(0,0,0,0.05);} .mention-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;margin-bottom:14px;} .mention-meta{font-size:12px;color:var(--muted);line-height:1.5;} .mention-title{font-weight:700;color:#1e293b;margin-bottom:6px;font-size:15px;} .mention-content{color:#334155;line-height:1.6;font-size:14px;margin-bottom:14px;} .mention-summary{background:#f8fafc;border-left:3px solid #cbd5e1;border-radius:0 8px 8px 0;padding:12px 16px;color:#475569;font-size:13px;line-height:1.6;} .link{color:#0ea5e9;text-decoration:none;font-weight:600;font-size:13px;} .link:hover{text-decoration:underline;}");
        sb.AppendLine(".footnote{margin-top:24px;color:var(--muted);font-size:13px;line-height:1.6;} .footer{margin-top:32px;text-align:center;color:var(--muted);font-size:13px;border-top:1px solid var(--line);padding-top:20px;}");
        sb.AppendLine("@media (max-width:920px){body{padding:16px;} .hero{padding:24px;} .two-col{grid-template-columns:1fr;} .section{padding:20px;}}");
        sb.AppendLine("@media print{");
        sb.AppendLine("  body{background:#fff;padding:0;} .page{max-width:none;}");
        sb.AppendLine("  .hero{background:#f8fafc!important;color:#0f172a!important;box-shadow:none;border:1px solid #e2e8f0;padding:24px;}");
        sb.AppendLine("  .hero:before,.hero:after{display:none;} .eyebrow{background:#e2e8f0;color:#475569;border-color:#cbd5e1;}");
        sb.AppendLine("  .hero p, .hero-meta{color:#475569!important;} .hero-meta{background:#f1f5f9;border:1px solid #e2e8f0;}");
        sb.AppendLine("  .hero .stat-card{background:#fff!important;border-color:#e2e8f0!important;} .hero .stat-value{color:#0f172a!important;} .hero .stat-label{color:#64748b!important;} .hero .stat-note{color:#64748b!important;}");
        sb.AppendLine("  .section{box-shadow:none;break-inside:avoid;page-break-inside:avoid;padding:24px 0;border:none;border-top:2px solid #f1f5f9;border-radius:0;margin-top:24px;}");
        sb.AppendLine("  .section:first-of-type{border-top:none;}");
        sb.AppendLine("  a{text-decoration:none;color:inherit;}");
        sb.AppendLine("  table, tr, td, th { page-break-inside: avoid; break-inside: avoid; }");
        sb.AppendLine("  .stat-card, .mention, .callout, .insight-list li { break-inside: avoid; page-break-inside: avoid; }");
        sb.AppendLine("  .two-col{display:flex;flex-direction:row;gap:24px;} .two-col > div{flex:1;}");
        sb.AppendLine("  .stats{grid-template-columns:repeat(3,1fr);gap:16px;}");
        sb.AppendLine("  .mentions{grid-template-columns:1fr;gap:20px;}");
        sb.AppendLine("  .mention{border:1px solid #e2e8f0;background:#fff;}");
        sb.AppendLine("}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class=\"page\">");
        sb.AppendLine("<section class=\"hero\"><div class=\"hero-inner\">");
        sb.AppendLine("<span class=\"eyebrow\">MCFH Report Center</span>");
        sb.AppendLine($"<h1>Báo cáo Social Listening cho {EscapeHtml(projectName)}</h1>");
        sb.AppendLine("<p>Tài liệu này tổng hợp các chỉ số quan trọng nhất để người dùng đọc nhanh bức tranh thảo luận, rủi ro sentiment, kênh nổi bật, influencer đáng chú ý và các vấn đề cần ưu tiên xử lý.</p>");
        sb.AppendLine($"<div class=\"hero-meta\"><span><strong>Dự án:</strong> {EscapeHtml(projectName)}</span><span><strong>Thời điểm tạo:</strong> {generated}</span><span><strong>Phạm vi dữ liệu:</strong> {FormatNumber(totalMentions)} mentions / {FormatNumber(totalComments)} bình luận</span></div>");
        sb.AppendLine("<div class=\"stats\">");
        sb.AppendLine(BuildStatCard("Tổng mentions", FormatNumber(totalMentions), "Khối lượng thảo luận đã thu thập từ các kênh đang theo dõi."));
        sb.AppendLine(BuildStatCard("Tổng bình luận", FormatNumber(totalComments), "Tổng phản hồi người dùng gắn với các mentions đã cào."));
        sb.AppendLine(BuildStatCard("Độ phủ AI", $"{coverage:0.#}%", pendingCount > 0 ? $"Còn {FormatNumber(pendingCount)} mention chưa có kết quả phân tích." : "Toàn bộ mentions hiện đã có kết quả phân tích."));
        sb.AppendLine(BuildStatCard("NSR Score", $"{(overview?.NsrScore ?? sentiment?.NsrScore ?? 0):+#.#;-#.#;0}%", "Chênh lệch tích cực so với tiêu cực trên phần dữ liệu đã phân tích."));
        sb.AppendLine(BuildStatCard("Sentiment chủ đạo", dominantSentiment, "Tông cảm xúc nổi bật nhất của cộng đồng ở thời điểm xuất báo cáo."));
        sb.AppendLine(topChannel != null
            ? BuildStatCard("Kênh dẫn đầu", EscapeHtml(topChannel.Label), $"{topChannel.MentionShare:0.#}% share of voice · {FormatNumber(topChannel.TotalComments)} bình luận.")
            : BuildStatCard("Kênh dẫn đầu", "Chưa đủ dữ liệu", "Hệ thống chưa có đủ mentions để xác định kênh nổi bật."));
        sb.AppendLine("</div></div></section>");

        sb.AppendLine("<section class=\"section\"><div class=\"two-col\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<h2>Tóm tắt điều hành</h2>");
        sb.AppendLine("<p class=\"sub\">Những điểm đáng chú ý nhất để đọc nhanh trước khi đi sâu vào chi tiết.</p>");
        sb.AppendLine("<ul class=\"insight-list\">");
        foreach (var insight in executiveInsights)
            sb.AppendLine($"<li>{EscapeHtml(insight)}</li>");
        sb.AppendLine("</ul></div>");
        sb.AppendLine("<div class=\"stack\">");
        sb.AppendLine("<div class=\"callout good\"><div class=\"callout-title\">Điểm sáng</div><div class=\"callout-body\">");
        sb.AppendLine(topChannel != null
            ? $"{EscapeHtml(topChannel.Label)} hiện tạo ra nhiều thảo luận nhất với {topChannel.MentionShare:0.#}% share of voice. Đây là kênh nên được dùng làm chuẩn để so sánh hiệu quả truyền thông."
            : "Chưa có đủ dữ liệu để xác định một kênh dẫn đầu rõ ràng.");
        sb.AppendLine("</div></div>");
        sb.AppendLine("<div class=\"callout bad\"><div class=\"callout-title\">Điểm cần theo dõi</div><div class=\"callout-body\">");
        sb.AppendLine(topRiskChannel != null && topRiskChannel.NegativePercent > 0
            ? $"{EscapeHtml(topRiskChannel.Label)} đang có tỷ lệ tiêu cực cao nhất ở mức {topRiskChannel.NegativePercent:0.#}% trên phần dữ liệu đã phân tích. Cần ưu tiên đọc kỹ các mentions và bình luận của kênh này."
            : "Chưa xuất hiện một kênh rủi ro quá nổi bật, nhưng vẫn nên theo dõi các mentions có lượng bình luận cao.");
        sb.AppendLine("</div></div>");
        sb.AppendLine("</div></div></section>");

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2>Gợi ý hành động</h2>");
        sb.AppendLine("<p class=\"sub\">Danh sách ưu tiên dành cho người dùng cuối hoặc đội vận hành sau khi đọc báo cáo.</p>");
        sb.AppendLine("<ul class=\"action-list\">");
        foreach (var action in actionItems)
            sb.AppendLine($"<li>{EscapeHtml(action)}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</section>");

        if (sentiment != null)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("<h2>Tình hình sentiment</h2>");
            sb.AppendLine("<p class=\"sub\">Tỷ lệ cảm xúc của tập mentions hiện tại, dùng để đánh giá mức độ ủng hộ, tranh luận hoặc rủi ro truyền thông.</p>");
            sb.AppendLine("<div class=\"stats\">");
            sb.AppendLine(BuildStatCard("Tích cực", $"{FormatNumber(sentiment.Positive)} ({sentiment.PositivePercent:0.#}%)", "Số lượng mentions nghiêng về đánh giá tốt hoặc phản hồi tích cực."));
            sb.AppendLine(BuildStatCard("Tiêu cực", $"{FormatNumber(sentiment.Negative)} ({sentiment.NegativePercent:0.#}%)", "Số lượng mentions có dấu hiệu phàn nàn, chỉ trích hoặc phản ứng xấu."));
            sb.AppendLine(BuildStatCard("Trung lập", $"{FormatNumber(sentiment.Neutral)} ({sentiment.NeutralPercent:0.#}%)", "Nhóm thảo luận mang tính thông tin hoặc chưa thể hiện thái độ rõ ràng."));
            sb.AppendLine(BuildStatCard("Chưa phân tích", FormatNumber(sentiment.Unanalyzed), sentiment.Unanalyzed > 0 ? "Nên tiếp tục chạy phân tích để tăng độ tin cậy cho các kết luận." : "Không còn mentions pending trong thời điểm xuất báo cáo."));
            sb.AppendLine("</div></section>");
        }

        if (channels?.Channels.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("<h2>Hiệu quả theo kênh</h2>");
            sb.AppendLine("<p class=\"sub\">So sánh quy mô thảo luận, chất lượng sentiment và độ phủ phân tích trên từng nền tảng để quyết định kênh nào cần ưu tiên theo dõi.</p>");
            sb.AppendLine("<table><thead><tr><th>Nền tảng</th><th>Mentions</th><th>% SOV</th><th>Bình luận</th><th>Độ phủ AI</th><th>NSR</th><th>Tích cực</th><th>Tiêu cực</th></tr></thead><tbody>");
            foreach (var ch in channels.Channels)
            {
                var channelAnalyzed = ch.Positive + ch.Negative + ch.Neutral;
                var channelCoverage = ch.Mentions > 0 ? Math.Round(channelAnalyzed * 100.0 / ch.Mentions, 1) : 0;
                sb.AppendLine(
                    $"<tr><td><strong>{EscapeHtml(ch.Label)}</strong></td><td class=\"num\">{FormatNumber(ch.Mentions)}</td><td class=\"num\">{ch.MentionShare:0.#}%</td><td class=\"num\">{FormatNumber(ch.TotalComments)}</td><td class=\"num\">{channelCoverage:0.#}%</td><td class=\"num\">{ch.NsrScore:+#.#;-#.#;0}%</td><td class=\"num\">{ch.PositivePercent:0.#}%</td><td class=\"num\">{ch.NegativePercent:0.#}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</section>");
        }

        if (influencers?.Influencers.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("<h2>Influencer / creator nổi bật</h2>");
            sb.AppendLine("<p class=\"sub\">Những tài khoản đang tạo ảnh hưởng lớn nhất dựa trên share of voice, lượng nhắc và lượng bình luận.</p>");
            sb.AppendLine("<table><thead><tr><th>Tên</th><th>Nền tảng</th><th>SOV</th><th>Mentions</th><th>Bình luận</th><th>Influence score</th><th>Sentiment chủ đạo</th></tr></thead><tbody>");
            foreach (var kol in influencers.Influencers.Take(8))
            {
                sb.AppendLine(
                    $"<tr><td><strong>{EscapeHtml(kol.Name)}</strong></td><td>{EscapeHtml(FormatPlatformLabel(kol.Platform))}</td><td class=\"num\">{kol.ShareOfVoice:0.#}%</td><td class=\"num\">{FormatNumber(kol.Mentions)}</td><td class=\"num\">{FormatNumber(kol.TotalComments)}</td><td class=\"num\">{kol.InfluenceScore:0.#}</td><td><span class=\"pill {GetSentimentCssClass(kol.DominantSentiment)}\">{EscapeHtml(FormatSentimentLabel(kol.DominantSentiment))}</span></td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</section>");
        }

        if (aspects?.Aspects.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("<h2>Khía cạnh người dùng đang bàn nhiều</h2>");
            sb.AppendLine("<p class=\"sub\">Danh sách chủ đề mà cộng đồng nhắc đến nhiều nhất và tông cảm xúc tương ứng để đội nội bộ hiểu điểm mạnh, điểm yếu đang nổi lên.</p>");
            sb.AppendLine("<table><thead><tr><th>Khía cạnh</th><th>Lượng nhắc</th><th>Tích cực</th><th>Tiêu cực</th><th>Trung lập</th></tr></thead><tbody>");
            foreach (var a in aspects.Aspects.OrderByDescending(a => a.TotalMentions).Take(8))
            {
                sb.AppendLine(
                    $"<tr><td><strong>{EscapeHtml(a.Label)}</strong></td><td class=\"num\">{FormatNumber(a.TotalMentions)}</td><td class=\"num\">{a.PositivePercent:0.#}%</td><td class=\"num\">{a.NegativePercent:0.#}%</td><td class=\"num\">{a.NeutralPercent:0.#}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</section>");
        }

        if (mentionHighlights.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("<h2>Mentions nổi bật cần đọc</h2>");
            sb.AppendLine("<p class=\"sub\">Ưu tiên các mentions có nhiều bình luận hoặc có dấu hiệu tiêu cực để người dùng có thể đi sâu ngay vào các trường hợp đáng quan tâm nhất.</p>");
            sb.AppendLine("<div class=\"mentions\">");
            foreach (var mention in mentionHighlights)
            {
                var stamp = mention.PostedAt ?? mention.ScrapedAt;
                var metaParts = new List<string>
                {
                    EscapeHtml(FormatPlatformLabel(mention.Platform)),
                    $"{FormatNumber(mention.CommentsCount)} bình luận"
                };
                if (!string.IsNullOrWhiteSpace(mention.AuthorName))
                    metaParts.Insert(0, EscapeHtml(mention.AuthorName));
                if (stamp.HasValue)
                    metaParts.Add(EscapeHtml(stamp.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))));

                sb.AppendLine("<article class=\"mention\">");
                sb.AppendLine("<div class=\"mention-head\">");
                sb.AppendLine("<div>");
                var titleText = EscapeHtml(mention.AuthorName ?? "Tác giả không rõ");
                if (mention.PinnedForReport) titleText = "📌 " + titleText;
                sb.AppendLine($"<div class=\"mention-title\">{titleText}</div>");
                sb.AppendLine($"<div class=\"mention-meta\">{string.Join(" · ", metaParts)}</div>");
                sb.AppendLine("</div>");
                sb.AppendLine($"<span class=\"pill {GetSentimentCssClass(mention.Sentiment)}\">{EscapeHtml(FormatSentimentLabel(mention.Sentiment))}</span>");
                sb.AppendLine("</div>");
                sb.AppendLine($"<div class=\"mention-content\">{EscapeHtml(ClipText(mention.Content, 260))}</div>");
                if (!string.IsNullOrWhiteSpace(mention.AiSummary))
                    sb.AppendLine($"<div class=\"mention-summary\">{EscapeHtml(mention.AiSummary)}</div>");
                if (!string.IsNullOrWhiteSpace(mention.OriginalUrl))
                    sb.AppendLine($"<div style=\"margin-top:12px;\"><a class=\"link\" href=\"{EscapeHtml(mention.OriginalUrl)}\">Mở bài gốc</a></div>");
                sb.AppendLine("</article>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</section>");
        }

        sb.AppendLine("<p class=\"footnote\">Ghi chú: báo cáo này được tổng hợp từ dữ liệu hiện có trong hệ thống tại thời điểm xuất file. Nếu AI Model không khả dụng, một phần sentiment có thể được sinh từ rule-based fallback để đảm bảo dashboard và báo cáo không bị gián đoạn.</p>");
        sb.AppendLine("<div class=\"footer\">Generated by MCFH Platform</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body></html>");

        return (sb.ToString(), "html", totalMentions);
    }

    private async Task<(byte[] Content, string Extension, int RowCount)> BuildAnalyticsPptxAsync(
        int workspaceId, int projectId, int userId, string projectName)
    {
        var overview = await _analytics.GetOverviewAsync(workspaceId, projectId, userId);
        var sentiment = await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId);
        var channels = await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId);
        var influencers = await _analytics.GetInfluencersAsync(workspaceId, projectId, userId);
        var aspects = await _analytics.GetAspectAnalysisAsync(workspaceId, projectId, userId);
        var generated = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));

        var slides = new List<PptxSlide>
        {
            new()
            {
                Heading = "Tổng quan chiến dịch (KPI)",
                Bullets =
                [
                    $"Tổng mentions: {FormatNumber(overview?.TotalMentions ?? 0)}",
                    $"Tổng bình luận: {FormatNumber(overview?.TotalComments ?? 0)}",
                    $"NSR Score: {(overview?.NsrScore ?? sentiment?.NsrScore ?? 0):+#.#;-#.#;0}%",
                    $"Tích cực: {FormatNumber(sentiment?.Positive ?? 0)} ({sentiment?.PositivePercent ?? 0:0.#}%)",
                    $"Tiêu cực: {FormatNumber(sentiment?.Negative ?? 0)} ({sentiment?.NegativePercent ?? 0:0.#}%)",
                    $"Chưa phân tích: {FormatNumber(sentiment?.Unanalyzed ?? 0)}"
                ]
            }
        };

        if (channels?.Channels.Count > 0)
        {
            slides.Add(new PptxSlide
            {
                Heading = "Hiệu quả theo kênh",
                Bullets = new List<string> { "Biểu đồ SOV (Share of Voice) theo nền tảng:" },
                ChartData = channels.Channels
                    .OrderByDescending(c => c.Mentions)
                    .Take(6)
                    .Select(c => new PptxBarChartItem
                    {
                        Label = c.Label,
                        Value = c.Mentions,
                        ValueLabel = $"{FormatNumber(c.Mentions)} ({c.MentionShare:0.#}%)",
                        ColorHex = c.Mentions > (overview?.TotalMentions * 0.5 ?? 0) ? "0EA5E9" : "10B981"
                    }).ToList()
            });
        }

        if (aspects?.Aspects.Count > 0)
        {
            slides.Add(new PptxSlide
            {
                Heading = "Các khía cạnh bàn luận nhiều nhất",
                Bullets = new List<string> { "AI phân tích các chủ đề người dùng thường đề cập:" },
                ChartData = aspects.Aspects
                    .OrderByDescending(a => a.TotalMentions)
                    .Take(6)
                    .Select(a => new PptxBarChartItem
                    {
                        Label = a.Label,
                        Value = a.TotalMentions,
                        ValueLabel = $"{FormatNumber(a.TotalMentions)} mentions",
                        ColorHex = a.NegativePercent > a.PositivePercent ? "EF4444" : "F59E0B"
                    }).ToList()
            });
        }

        if (influencers?.Influencers.Count > 0)
        {
            slides.Add(new PptxSlide
            {
                Heading = "Top Influencers Nổi Bật",
                Bullets = influencers.Influencers
                    .OrderByDescending(i => i.InfluenceScore)
                    .Take(6)
                    .Select(i => $"{i.Name} ({i.Platform}): {FormatNumber(i.Mentions)} mentions · {FormatNumber(i.TotalComments)} bình luận · Điểm ảnh hưởng: {i.InfluenceScore:0.#}")
                    .ToList()
            });
        }

        slides.Add(new PptxSlide
        {
            Heading = "Ghi chú & Nguồn Dữ Liệu",
            Bullets =
            [
                $"Thời điểm xuất: {generated}",
                "Báo cáo được tổng hợp tự động từ AI qua hệ thống MCFH.",
                "Dữ liệu biểu đồ được trích xuất dựa trên tập mentions hiện có."
            ]
        });

        var bytes = SimplePptxBuilder.Build($"Báo cáo — {projectName}", slides);
        return (bytes, "pptx", overview?.TotalMentions ?? 0);
    }

    private async Task<(byte[] Content, string Extension, int RowCount)> BuildAnalyticsPdfAsync(
        int workspaceId, int projectId, int userId, string projectName, MentionQueryDto? filter = null)
    {
        var (html, _, rowCount) = await BuildAnalyticsHtmlAsync(workspaceId, projectId, userId, projectName, filter);

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

    /// <summary>Tính lại overview KPI từ tập mentions đã lọc (bespoke) — không đụng tới feedbacks chưa lọc của project.</summary>
    private static ProjectOverviewDto BuildOverviewFromMentions(int projectId, string projectName, List<MentionDto> mentions)
    {
        var counts = CountSentiments(mentions);
        var analyzed = mentions.Count(m => m.IsAnalyzed);

        return new ProjectOverviewDto
        {
            ProjectId = projectId,
            ProjectName = projectName,
            TotalMentions = mentions.Count,
            TotalComments = mentions.Sum(m => m.CommentsCount),
            AnalyzedCount = analyzed,
            PendingAnalysisCount = mentions.Count - analyzed,
            NsrScore = counts.NsrScore,
            PositiveCount = counts.Positive,
            NegativeCount = counts.Negative,
            NeutralCount = counts.Neutral,
            PlatformBreakdown = mentions
                .GroupBy(m => m.Platform ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static SentimentSummaryDto BuildSentimentFromMentions(List<MentionDto> mentions)
    {
        var total = mentions.Count;
        var counts = CountSentiments(mentions);
        var unanalyzed = total - counts.Positive - counts.Negative - counts.Neutral;

        return new SentimentSummaryDto
        {
            Total = total,
            Positive = counts.Positive,
            Negative = counts.Negative,
            Neutral = counts.Neutral,
            Unanalyzed = unanalyzed,
            PositivePercent = total > 0 ? Math.Round(counts.Positive * 100.0 / total, 1) : 0,
            NegativePercent = total > 0 ? Math.Round(counts.Negative * 100.0 / total, 1) : 0,
            NeutralPercent = total > 0 ? Math.Round(counts.Neutral * 100.0 / total, 1) : 0,
            NsrScore = counts.NsrScore
        };
    }

    private static ChannelComparisonDto BuildChannelsFromMentions(List<MentionDto> mentions)
    {
        var totalMentions = mentions.Count;
        var totalComments = mentions.Sum(m => m.CommentsCount);
        var platformOrder = new[] { "facebook", "youtube", "tiktok", "news", "threads" };

        var channels = mentions
            .GroupBy(m => (m.Platform ?? "unknown").ToLowerInvariant())
            .Select(g =>
            {
                var counts = CountSentiments(g);
                var mentionsCount = g.Count();
                var comments = g.Sum(m => m.CommentsCount);
                var analyzed = counts.Positive + counts.Negative + counts.Neutral;

                return new ChannelStatsDto
                {
                    Platform = g.Key,
                    Label = FormatPlatformLabel(g.Key),
                    Mentions = mentionsCount,
                    MentionShare = totalMentions > 0 ? Math.Round(mentionsCount * 100.0 / totalMentions, 1) : 0,
                    TotalComments = comments,
                    CommentShare = totalComments > 0 ? Math.Round(comments * 100.0 / totalComments, 1) : 0,
                    Positive = counts.Positive,
                    Negative = counts.Negative,
                    Neutral = counts.Neutral,
                    Unanalyzed = mentionsCount - analyzed,
                    NsrScore = counts.NsrScore,
                    PositivePercent = analyzed > 0 ? Math.Round(counts.Positive * 100.0 / analyzed, 1) : 0,
                    NegativePercent = analyzed > 0 ? Math.Round(counts.Negative * 100.0 / analyzed, 1) : 0,
                    NeutralPercent = analyzed > 0 ? Math.Round(counts.Neutral * 100.0 / analyzed, 1) : 0
                };
            })
            .OrderBy(c =>
            {
                var idx = Array.IndexOf(platformOrder, c.Platform);
                return idx >= 0 ? idx : 99;
            })
            .ThenByDescending(c => c.Mentions)
            .ToList();

        return new ChannelComparisonDto
        {
            TotalMentions = totalMentions,
            TotalComments = totalComments,
            Channels = channels
        };
    }

    /// <summary>Bảng influencer đơn giản hoá từ mentions đã lọc: Name, Platform, Mentions — đủ dùng cho báo cáo bespoke.</summary>
    private static InfluencerAnalyticsDto BuildInfluencersFromMentions(List<MentionDto> mentions)
    {
        var totalMentions = mentions.Count;
        var groups = new Dictionary<string, FilteredInfluencerAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in mentions)
        {
            var platform = (m.Platform ?? "unknown").ToLowerInvariant();
            var name = string.IsNullOrWhiteSpace(m.AuthorName) ? "Không rõ" : m.AuthorName.Trim();
            var key = $"{platform}|{name}";

            if (!groups.TryGetValue(key, out var acc))
            {
                acc = new FilteredInfluencerAccumulator { Name = name, Platform = platform };
                groups[key] = acc;
            }

            acc.Mentions++;
            acc.TotalComments += m.CommentsCount;
            switch (m.Sentiment?.ToLowerInvariant())
            {
                case "positive": acc.Positive++; break;
                case "negative": acc.Negative++; break;
                case "neutral": acc.Neutral++; break;
            }
        }

        var influencers = groups.Values
            .Select(acc =>
            {
                var score = acc.Mentions * 10.0 + acc.TotalComments;
                if (acc.Positive > acc.Negative) score += acc.Positive * 2;
                if (acc.Negative > acc.Positive) score -= acc.Negative;

                return new InfluencerDto
                {
                    Id = $"{acc.Platform}|{acc.Name}",
                    Name = acc.Name,
                    Platform = acc.Platform,
                    Mentions = acc.Mentions,
                    TotalComments = acc.TotalComments,
                    ShareOfVoice = totalMentions > 0 ? Math.Round(acc.Mentions * 100.0 / totalMentions, 1) : 0,
                    InfluenceScore = Math.Round(Math.Max(0, score), 1),
                    DominantSentiment = ResolveDominantSentiment(acc.Positive, acc.Negative, acc.Neutral),
                    PositiveCount = acc.Positive,
                    NegativeCount = acc.Negative,
                    NeutralCount = acc.Neutral
                };
            })
            .OrderByDescending(i => i.InfluenceScore)
            .ThenByDescending(i => i.Mentions)
            .ToList();

        return new InfluencerAnalyticsDto
        {
            TotalMentions = totalMentions,
            UniqueInfluencers = influencers.Count,
            Influencers = influencers
        };
    }

    private sealed class FilteredInfluencerAccumulator
    {
        public string Name { get; set; } = "";
        public string Platform { get; set; } = "";
        public int Mentions { get; set; }
        public int TotalComments { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
    }

    private static string? ResolveDominantSentiment(int positive, int negative, int neutral)
    {
        if (positive == 0 && negative == 0 && neutral == 0) return null;
        if (positive >= negative && positive >= neutral) return "positive";
        if (negative >= positive && negative >= neutral) return "negative";
        return "neutral";
    }

    private static (int Positive, int Negative, int Neutral, double NsrScore) CountSentiments(IEnumerable<MentionDto> mentions)
    {
        var positive = 0;
        var negative = 0;
        var neutral = 0;

        foreach (var m in mentions)
        {
            switch (m.Sentiment?.ToLowerInvariant())
            {
                case "positive": positive++; break;
                case "negative": negative++; break;
                case "neutral": neutral++; break;
            }
        }

        var analyzed = positive + negative + neutral;
        var nsr = analyzed > 0 ? Math.Round((positive - negative) * 100.0 / analyzed, 1) : 0;
        return (positive, negative, neutral, nsr);
    }

    private static List<string> BuildExecutiveInsights(
        int totalMentions,
        int totalComments,
        int pendingCount,
        double coverage,
        string dominantSentiment,
        ChannelStatsDto? topChannel,
        ChannelStatsDto? topRiskChannel,
        InfluencerDto? topInfluencer,
        AspectAnalysisDto? aspects)
    {
        var insights = new List<string>
        {
            $"Hệ thống đang tổng hợp {FormatNumber(totalMentions)} mentions và {FormatNumber(totalComments)} bình luận cho dự án ở thời điểm xuất báo cáo.",
            $"Độ phủ phân tích hiện đạt {coverage:0.#}% và sentiment chủ đạo của cộng đồng đang nghiêng về hướng {dominantSentiment.ToLowerInvariant()}."
        };

        if (topChannel != null)
            insights.Add($"{topChannel.Label} là kênh dẫn đầu với {topChannel.MentionShare:0.#}% share of voice và {FormatNumber(topChannel.TotalComments)} bình luận, phù hợp để dùng làm kênh benchmark chính.");

        if (topRiskChannel != null && topRiskChannel.NegativePercent > 0)
            insights.Add($"{topRiskChannel.Label} đang có tỷ lệ tiêu cực cao nhất ở mức {topRiskChannel.NegativePercent:0.#}%, đây là kênh cần ưu tiên theo dõi rủi ro.");

        if (topInfluencer != null)
            insights.Add($"{topInfluencer.Name} đang là creator có ảnh hưởng nổi bật nhất với score {topInfluencer.InfluenceScore:0.#} và {topInfluencer.ShareOfVoice:0.#}% share of voice.");

        if (!string.IsNullOrWhiteSpace(aspects?.TopNegativeAspect))
            insights.Add($"Khía cạnh bị phàn nàn nổi bật nhất hiện tại là {aspects.TopNegativeAspect}, nên được dùng làm điểm vào để đọc sâu các phản hồi tiêu cực.");

        if (pendingCount > 0)
            insights.Add($"Vẫn còn {FormatNumber(pendingCount)} mention chưa có sentiment, vì vậy các kết luận hiện tại nên được xem là gần đúng thay vì tuyệt đối.");

        return insights;
    }

    private static List<string> BuildActionItems(
        int pendingCount,
        ChannelStatsDto? topRiskChannel,
        InfluencerDto? topInfluencer,
        AspectAnalysisDto? aspects)
    {
        var items = new List<string>();

        if (pendingCount > 0)
            items.Add($"Chạy phân tích bổ sung cho {FormatNumber(pendingCount)} mention còn pending để tăng độ tin cậy trước khi dùng báo cáo cho quyết định quan trọng.");

        if (topRiskChannel != null && topRiskChannel.NegativePercent > 0)
            items.Add($"Đọc kỹ các mentions trên {topRiskChannel.Label} vì đây là kênh có tỷ lệ tiêu cực cao nhất và dễ phát sinh issue truyền thông nhất.");

        if (topInfluencer != null)
            items.Add($"Theo dõi sát creator {topInfluencer.Name} vì đây là nguồn ảnh hưởng lớn nhất trong tập dữ liệu hiện tại, đặc biệt khi sentiment của creator này đổi chiều.");

        if (!string.IsNullOrWhiteSpace(aspects?.TopNegativeAspect))
            items.Add($"Ưu tiên kiểm tra nguyên nhân gốc liên quan đến khía cạnh {aspects.TopNegativeAspect} để xác định cần phản hồi truyền thông hay cải thiện vận hành.");

        if (items.Count == 0)
            items.Add("Dữ liệu hiện khá ổn định. Nên tiếp tục theo dõi định kỳ và so sánh báo cáo này với kỳ sau để phát hiện biến động sớm.");

        return items;
    }

    private static string BuildStatCard(string label, string value, string note) =>
        $"<div class=\"stat-card\"><div class=\"stat-label\">{EscapeHtml(label)}</div><div class=\"stat-value\">{EscapeHtml(value)}</div><div class=\"stat-note\">{EscapeHtml(note)}</div></div>";

    private static string ResolveDominantSentiment(SentimentSummaryDto? sentiment)
    {
        if (sentiment == null) return "Trung lập";

        var map = new Dictionary<string, int>
        {
            ["Tích cực"] = sentiment.Positive,
            ["Tiêu cực"] = sentiment.Negative,
            ["Trung lập"] = sentiment.Neutral
        };

        return map.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "Trung lập";
    }

    private static string FormatSentimentLabel(string? sentiment) =>
        sentiment?.Trim().ToLowerInvariant() switch
        {
            "positive" => "Tích cực",
            "negative" => "Tiêu cực",
            "neutral" => "Trung lập",
            null or "" => "Chưa phân tích",
            _ => sentiment
        };

    private static string GetSentimentCssClass(string? sentiment) =>
        sentiment?.Trim().ToLowerInvariant() switch
        {
            "positive" => "positive",
            "negative" => "negative",
            "neutral" => "neutral",
            _ => "pending"
        };

    private static string ClipText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Không có nội dung hiển thị.";
        var value = text.Trim();
        return value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "...";
    }

    private static string FormatNumber(int value) =>
        value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    private static string FormatPlatformLabel(string? platform) =>
        platform?.Trim().ToLowerInvariant() switch
        {
            "facebook" => "Facebook",
            "youtube" => "YouTube",
            "tiktok" => "TikTok",
            "threads" => "Threads",
            "news" => "Tin tức",
            null or "" => "Unknown",
            _ => CultureInfo.GetCultureInfo("vi-VN").TextInfo.ToTitleCase(platform.ToLowerInvariant())
        };

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
