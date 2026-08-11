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

    /// <summary>
    /// PDF kiểu slide 16:9 dành riêng báo cáo chuyên sâu (bespoke).
    /// Cấu trúc bắt buộc: Tổng quan / Phân tích / Khuyến nghị. Không ghi vào Report Center.
    /// </summary>
    public async Task<(byte[] Content, string FileName)?> RenderBespokeSlidePdfAsync(
        int workspaceId, int projectId, int userId, string? displayName = null,
        MentionQueryDto? filter = null,
        string? keyword = null,
        string? dateFrom = null,
        string? dateTo = null)
    {
        var project = await GetProjectAsync(workspaceId, projectId, userId);
        if (project == null) return null;

        var titleName = string.IsNullOrWhiteSpace(displayName) ? project.Name : displayName.Trim();
        try
        {
            var (pdfBytes, _, _) = await BuildBespokeSlidePdfAsync(
                workspaceId, projectId, userId, titleName, filter, keyword, dateFrom, dateTo);
            var fileName = $"{SanitizeFileName($"Bao-cao-chuyen-sau-{titleName}")}.pdf";
            return (pdfBytes, fileName);
        }
        catch (PlaywrightException ex)
        {
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
            mentions = await _analytics.GetMentionsAsync(workspaceId, projectId, userId, filter);
            overview = BuildOverviewFromMentions(projectId, projectName, mentions);
            sentiment = BuildSentimentFromMentions(mentions);
            channels = BuildChannelsFromMentions(mentions);
            influencers = BuildInfluencersFromMentions(mentions);
            aspects = null;
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

        var totalMentions = overview?.TotalMentions ?? 0;
        var totalComments = overview?.TotalComments ?? 0;
        var pendingCount = overview?.PendingAnalysisCount ?? 0;
        var coverage = totalMentions > 0 ? (totalMentions - pendingCount) * 100.0 / totalMentions : 0;
        var nsrScore = overview?.NsrScore ?? 0;
        var generated = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        var dominantSentiment = ResolveDominantSentiment(overview?.PositiveCount ?? 0, overview?.NegativeCount ?? 0, overview?.NeutralCount ?? 0) ?? "N/A";
        var topChannel = channels?.Channels.FirstOrDefault();
        var topRiskChannel = channels?.Channels.OrderByDescending(c => c.NegativePercent).FirstOrDefault();
        var topInfluencer = influencers?.Influencers.FirstOrDefault();
        
        var topChannelInfo = topChannel != null ? $"{topChannel.Label} ({topChannel.MentionShare:0.#}% SOV)" : "N/A";
        var topNegativeAspects = aspects?.Aspects.OrderByDescending(a => a.Negative).Take(3).Select(a => a.Label).ToList();
        var topNegStr = topNegativeAspects != null && topNegativeAspects.Any() ? string.Join(", ", topNegativeAspects) : "Không có";

        var pinnedQuotes = mentions.Where(m => m.PinnedForReport).Select(m => m.Content ?? "").Where(c => !string.IsNullOrWhiteSpace(c)).Take(5).ToList();

        var aiInsights = await _aiSentiment.GenerateReportInsightsAsync(
            projectName, totalMentions, nsrScore, topChannelInfo, topNegStr, pinnedQuotes);

        var executiveInsights = aiInsights?.ExecutiveInsights ?? BuildExecutiveInsights(
                totalMentions, totalComments, pendingCount, coverage, dominantSentiment,
                topChannel, topRiskChannel, topInfluencer, aspects);

        var actionItems = aiInsights?.ActionItems ?? BuildActionItems(pendingCount, topRiskChannel, topInfluencer, aspects);
        
        var nsrComment = !string.IsNullOrWhiteSpace(aiInsights?.NsrComment) ? aiInsights.NsrComment : "Chênh lệch tích cực so với tiêu cực.";
        var sentimentAnalysis = !string.IsNullOrWhiteSpace(aiInsights?.SentimentAnalysis) ? aiInsights.SentimentAnalysis : "Phân tích cảm xúc đang được AI túm gọn.";
        var channelAnalysis = !string.IsNullOrWhiteSpace(aiInsights?.ChannelAnalysis) ? aiInsights.ChannelAnalysis : "Các kênh thảo luận cho thấy mức độ tương tác cao.";
        var influencerAnalysis = !string.IsNullOrWhiteSpace(aiInsights?.InfluencerAnalysis) ? aiInsights.InfluencerAnalysis : "Các KOLs/KOCs đang đóng vai trò quan trọng trong việc dẫn dắt luồng thảo luận.";
        var swot = aiInsights?.SwotAnalysis;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Báo cáo — {EscapeHtml(projectName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(":root { --bg: #f8fafc; --panel: #ffffff; --text: #0f172a; --muted: #64748b; --brand: #1e3a8a; --brand-2: #3b82f6; --accent: #f59e0b; }");
        sb.AppendLine("* { box-sizing: border-box; }");
        sb.AppendLine("body { font-family: 'Inter', system-ui, sans-serif; background: #e2e8f0; margin: 0; padding: 0; -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
        sb.AppendLine(".slide { width: 1920px; height: 1080px; background: #ffffff; position: relative; overflow: hidden; page-break-after: always; display: flex; flex-direction: column; }");
        sb.AppendLine(".slide-bg-shape { position: absolute; top: -20%; right: -10%; width: 1200px; height: 1200px; background: radial-gradient(circle, rgba(59,130,246,0.08) 0%, rgba(255,255,255,0) 70%); border-radius: 50%; z-index: 0; }");
        sb.AppendLine(".slide-header { height: 140px; display: flex; align-items: center; justify-content: space-between; padding: 0 80px; border-bottom: 2px solid #f1f5f9; z-index: 1; }");
        sb.AppendLine(".slide-header h1 { font-size: 38px; font-weight: 800; color: var(--brand); text-transform: uppercase; margin: 0; }");
        sb.AppendLine(".slide-header .brand { font-size: 36px; font-weight: 900; color: var(--brand-2); display: flex; align-items: center; gap: 12px; }");
        sb.AppendLine(".slide-content { flex: 1; display: flex; padding: 60px 80px; gap: 60px; z-index: 1; }");
        sb.AppendLine(".slide-col-left { flex: 1; display: flex; flex-direction: column; }");
        sb.AppendLine(".slide-col-right { flex: 1.2; background: #ffffff; border: 1px solid #f1f5f9; border-radius: 32px; box-shadow: 0 20px 40px rgba(0,0,0,0.04); padding: 50px; display: flex; flex-direction: column; justify-content: center; }");
        sb.AppendLine("h2.section-title { font-size: 54px; font-weight: 800; color: var(--brand); margin: 0 0 24px; line-height: 1.3; }");
        sb.AppendLine(".ai-box { background: linear-gradient(145deg, #f8fafc, #f1f5f9); border-left: 8px solid var(--brand-2); padding: 36px; border-radius: 16px; font-size: 26px; line-height: 1.6; color: var(--text); box-shadow: 0 10px 20px rgba(0,0,0,0.02); }");
        sb.AppendLine(".ai-box p { margin: 0 0 16px; } .ai-box p:last-child { margin: 0; }");
        sb.AppendLine(".stats-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-top: 40px; }");
        sb.AppendLine(".stat-box { background: #fff; border: 2px solid #e2e8f0; border-radius: 20px; padding: 32px; display: flex; flex-direction: column; justify-content: center; }");
        sb.AppendLine(".stat-box.highlight { background: linear-gradient(135deg, var(--brand), var(--brand-2)); color: #fff; border: none; }");
        sb.AppendLine(".stat-box.highlight .stat-val { color: #fff; }");
        sb.AppendLine(".stat-box.highlight .stat-label { color: rgba(255,255,255,0.8); }");
        sb.AppendLine(".stat-label { font-size: 20px; font-weight: 700; color: var(--muted); text-transform: uppercase; margin-bottom: 12px; }");
        sb.AppendLine(".stat-val { font-size: 64px; font-weight: 900; color: var(--brand); line-height: 1; }");
        sb.AppendLine(".stat-note { font-size: 18px; color: var(--muted); margin-top: 12px; line-height: 1.4; }");
        sb.AppendLine(".stat-box.highlight .stat-note { color: rgba(255,255,255,0.7); }");
        sb.AppendLine("table.slide-table { width: 100%; border-collapse: collapse; }");
        sb.AppendLine("table.slide-table th { font-size: 22px; color: var(--muted); padding: 24px 12px; border-bottom: 3px solid #e2e8f0; text-align: left; font-weight: 700; text-transform: uppercase; }");
        sb.AppendLine("table.slide-table td { font-size: 26px; color: var(--text); padding: 24px 12px; border-bottom: 1px solid #f1f5f9; font-weight: 500; }");
        sb.AppendLine("ul.insight-list { font-size: 26px; line-height: 1.6; color: var(--text); padding-left: 32px; margin: 0; }");
        sb.AppendLine("ul.insight-list li { margin-bottom: 24px; }");
        sb.AppendLine(".swot-grid { display: grid; grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr; gap: 32px; height: 100%; }");
        sb.AppendLine(".swot-item { border-radius: 24px; padding: 40px; }");
        sb.AppendLine(".swot-item h3 { font-size: 32px; font-weight: 800; margin: 0 0 24px; display: flex; align-items: center; gap: 12px; }");
        sb.AppendLine(".swot-item.s { background: #ecfdf5; border: 2px solid #a7f3d0; } .swot-item.s h3 { color: #059669; }");
        sb.AppendLine(".swot-item.w { background: #fef2f2; border: 2px solid #fecaca; } .swot-item.w h3 { color: #dc2626; }");
        sb.AppendLine(".swot-item.o { background: #fffbeb; border: 2px solid #fde68a; } .swot-item.o h3 { color: #d97706; }");
        sb.AppendLine(".swot-item.t { background: #eff6ff; border: 2px solid #bfdbfe; } .swot-item.t h3 { color: #2563eb; }");
        sb.AppendLine(".swot-item ul { font-size: 22px; line-height: 1.6; padding-left: 24px; margin: 0; color: #334155; }");
        sb.AppendLine("</style></head><body>");

        string GenerateQuickChartUrl(object config, int width, int height)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            var encoded = Uri.EscapeDataString(json);
            return $"https://quickchart.io/chart?c={encoded}&w={width}&h={height}&bkg=transparent";
        }

        // Helper cho header slide
        string BuildSlideHeader(string prefix, string title)
        {
            return $"<div class=\"slide-header\"><h1><span style=\"color:var(--muted); font-weight:400;\">{prefix} |</span> {title}</h1><div class=\"brand\">MCFH AI Analytics</div></div>";
        }

        // SLIDE 1: TÓM TẮT ĐIỀU HÀNH
        sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
        sb.AppendLine(BuildSlideHeader("01", "TÓM TẮT ĐIỀU HÀNH (EXECUTIVE SUMMARY)"));
        sb.AppendLine("<div class=\"slide-content\">");
        sb.AppendLine("<div class=\"slide-col-left\">");
        sb.AppendLine($"<h2 class=\"section-title\">Bức tranh toàn cảnh: {EscapeHtml(projectName)}</h2>");
        sb.AppendLine("<div class=\"ai-box\">");
        foreach(var item in executiveInsights) sb.AppendLine($"<p>• {EscapeHtml(item)}</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"slide-col-right\" style=\"background:transparent; box-shadow:none; border:none; padding:0;\">");
        sb.AppendLine("<div class=\"stats-grid\" style=\"margin-top:0; grid-template-rows: 1fr 1fr; height: 100%;\">");
        sb.AppendLine($"<div class=\"stat-box highlight\"><div class=\"stat-label\">Tổng Mentions</div><div class=\"stat-val\">{FormatNumber(totalMentions)}</div><div class=\"stat-note\">Từ các kênh đang theo dõi</div></div>");
        sb.AppendLine($"<div class=\"stat-box\"><div class=\"stat-label\">Tổng tương tác (Bình luận)</div><div class=\"stat-val\">{FormatNumber(totalComments)}</div><div class=\"stat-note\">Lượng phản hồi trực tiếp</div></div>");
        sb.AppendLine($"<div class=\"stat-box\"><div class=\"stat-label\">Chỉ số NSR</div><div class=\"stat-val\" style=\"color:{(nsrScore >= 0 ? "#10b981" : "#ef4444")}\">{nsrScore:+#.#;-#.#;0}%</div><div class=\"stat-note\">{EscapeHtml(nsrComment)}</div></div>");
        sb.AppendLine($"<div class=\"stat-box\"><div class=\"stat-label\">Kênh dẫn đầu</div><div class=\"stat-val\" style=\"font-size:42px;\">{EscapeHtml(topChannel?.Label ?? "N/A")}</div><div class=\"stat-note\">Chiếm {topChannel?.MentionShare:0.#}% thảo luận</div></div>");
        sb.AppendLine("</div></div></div></div>");

        // SLIDE 2: TÌNH HÌNH SENTIMENT
        if (sentiment != null)
        {
            var sentimentChartConfig = new {
                type = "doughnut",
                data = new {
                    labels = new[] { "Tích cực", "Tiêu cực", "Trung lập" },
                    datasets = new[] {
                        new {
                            data = new[] { sentiment.Positive, sentiment.Negative, sentiment.Neutral },
                            backgroundColor = new[] { "#10B981", "#EF4444", "#64748B" },
                            borderWidth = 0
                        }
                    }
                },
                options = new {
                    cutoutPercentage = 65,
                    layout = new { padding = 20 },
                    legend = new { position = "right", labels = new { fontSize = 28, fontColor = "#0f172a", padding = 30, boxWidth = 24 } },
                    plugins = new {
                        datalabels = new { display = true, color = "#fff", font = new { weight = "bold", size = 26 } },
                        doughnutlabel = new {
                            labels = new object[] {
                                new { text = FormatNumber(sentiment.Total).ToString(), font = new { size = 56, weight = "bold", family = "sans-serif" }, color = "#0f172a" },
                                new { text = "Mentions", font = new { size = 24, weight = "normal", family = "sans-serif" }, color = "#64748b" }
                            }
                        }
                    }
                }
            };
            
            sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
            sb.AppendLine(BuildSlideHeader("02", "PHÂN TÍCH CẢM XÚC (SENTIMENT ANALYSIS)"));
            sb.AppendLine("<div class=\"slide-content\">");
            sb.AppendLine("<div class=\"slide-col-left\">");
            sb.AppendLine($"<h2 class=\"section-title\">Chỉ số cảm xúc thương hiệu</h2>");
            sb.AppendLine($"<div class=\"ai-box\"><p>{EscapeHtml(sentimentAnalysis)}</p></div>");
            sb.AppendLine("<div class=\"stats-grid\">");
            sb.AppendLine($"<div class=\"stat-box\"><div class=\"stat-label\">Tích cực</div><div class=\"stat-val\" style=\"color:#10b981;\">{sentiment.PositivePercent:0.#}%</div></div>");
            sb.AppendLine($"<div class=\"stat-box\"><div class=\"stat-label\">Tiêu cực</div><div class=\"stat-val\" style=\"color:#ef4444;\">{sentiment.NegativePercent:0.#}%</div></div>");
            sb.AppendLine("</div></div>");
            sb.AppendLine("<div class=\"slide-col-right\">");
            sb.AppendLine($"<img src=\"{GenerateQuickChartUrl(sentimentChartConfig, 800, 600)}\" style=\"width:100%; display:block; margin:auto;\" />");
            sb.AppendLine("</div></div></div>");
        }

        // SLIDE 3: HIỆU QUẢ KÊNH
        if (channels?.Channels.Count > 0)
        {
            var channelChartConfig = new {
                type = "horizontalBar",
                data = new {
                    labels = channels.Channels.Take(5).Select(c => c.Label).ToArray(),
                    datasets = new[] {
                        new {
                            label = "Mentions",
                            data = channels.Channels.Take(5).Select(c => c.Mentions).ToArray(),
                            backgroundColor = "rgba(59, 130, 246, 0.8)",
                            borderColor = "#2563eb",
                            borderWidth = 2,
                            borderRadius = 8
                        }
                    }
                },
                options = new {
                    legend = new { display = false },
                    plugins = new { datalabels = new { align = "end", anchor = "end", color = "#0f172a", font = new { size = 22, weight = "bold" } } },
                    scales = new { 
                        xAxes = new[] { new { ticks = new { beginAtZero = true, fontSize = 20 }, gridLines = new { display = false } } },
                        yAxes = new[] { new { ticks = new { fontSize = 24, fontColor = "#0f172a", fontStyle="bold" }, gridLines = new { display = false } } }
                    },
                    layout = new { padding = new { right = 60 } }
                }
            };
            
            sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
            sb.AppendLine(BuildSlideHeader("03", "HIỆU QUẢ THEO KÊNH (CHANNEL PERFORMANCE)"));
            sb.AppendLine("<div class=\"slide-content\">");
            sb.AppendLine("<div class=\"slide-col-left\">");
            sb.AppendLine($"<h2 class=\"section-title\">Phân bổ thảo luận theo nền tảng</h2>");
            sb.AppendLine($"<div class=\"ai-box\"><p>{EscapeHtml(channelAnalysis)}</p></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"slide-col-right\">");
            sb.AppendLine($"<img src=\"{GenerateQuickChartUrl(channelChartConfig, 900, 500)}\" style=\"width:100%; display:block; margin-bottom: 40px;\" />");
            sb.AppendLine("<table class=\"slide-table\"><thead><tr><th>Nền tảng</th><th>Mentions</th><th>% SOV</th><th>NSR</th></tr></thead><tbody>");
            foreach (var ch in channels.Channels.Take(3))
            {
                sb.AppendLine($"<tr><td>{EscapeHtml(ch.Label)}</td><td>{FormatNumber(ch.Mentions)}</td><td>{ch.MentionShare:0.#}%</td><td style=\"color:{(ch.NsrScore >= 0 ? "#10b981" : "#ef4444")}\">{ch.NsrScore:+#.#;-#.#;0}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div></div></div>");
        }
        
        // SLIDE 4: INFLUENCER
        if (influencers?.Influencers.Count > 0)
        {
            sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
            sb.AppendLine(BuildSlideHeader("04", "NGƯỜI ẢNH HƯỞNG (TOP INFLUENCERS)"));
            sb.AppendLine("<div class=\"slide-content\">");
            sb.AppendLine("<div class=\"slide-col-left\">");
            sb.AppendLine($"<h2 class=\"section-title\">Các nhân tố dẫn dắt luồng thảo luận</h2>");
            sb.AppendLine($"<div class=\"ai-box\"><p>{EscapeHtml(influencerAnalysis)}</p></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"slide-col-right\" style=\"justify-content: flex-start;\">");
            sb.AppendLine("<table class=\"slide-table\"><thead><tr><th>Tên tài khoản</th><th>Nền tảng</th><th>Mentions</th><th>Bình luận</th><th>NSR</th></tr></thead><tbody>");
            foreach (var kol in influencers.Influencers.Take(6))
            {
                var nsr = ResolveDominantSentiment(kol.PositiveCount, kol.NegativeCount, kol.NeutralCount);
                var nsrColor = nsr == "Tích cực" ? "#10b981" : (nsr == "Tiêu cực" ? "#ef4444" : "#64748b");
                sb.AppendLine($"<tr><td>{EscapeHtml(kol.Name)}</td><td>{EscapeHtml(FormatPlatformLabel(kol.Platform))}</td><td>{FormatNumber(kol.Mentions)}</td><td>{FormatNumber(kol.TotalComments)}</td><td style=\"color:{nsrColor};\">{nsr}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div></div></div>");
        }

        // SLIDE 5: SWOT
        if (swot != null)
        {
            sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
            sb.AppendLine(BuildSlideHeader("05", "PHÂN TÍCH SWOT (AI GENERATED)"));
            sb.AppendLine("<div class=\"slide-content\" style=\"padding: 40px 80px;\">");
            sb.AppendLine("<div class=\"swot-grid\">");
            
            sb.AppendLine("<div class=\"swot-item s\"><h3>💪 Điểm mạnh (Strengths)</h3><ul>");
            foreach (var item in swot.Strengths ?? new List<string>()) sb.AppendLine($"<li>{EscapeHtml(item)}</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("<div class=\"swot-item w\"><h3>⚠️ Điểm yếu (Weaknesses)</h3><ul>");
            foreach (var item in swot.Weaknesses ?? new List<string>()) sb.AppendLine($"<li>{EscapeHtml(item)}</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("<div class=\"swot-item o\"><h3>🚀 Cơ hội (Opportunities)</h3><ul>");
            foreach (var item in swot.Opportunities ?? new List<string>()) sb.AppendLine($"<li>{EscapeHtml(item)}</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("<div class=\"swot-item t\"><h3>⚡ Thách thức (Threats)</h3><ul>");
            foreach (var item in swot.Threats ?? new List<string>()) sb.AppendLine($"<li>{EscapeHtml(item)}</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("</div></div></div>");
        }

        // SLIDE 6: CHIẾN LƯỢC
        if (aiInsights?.MarketingStrategies?.Count > 0 || actionItems.Count > 0)
        {
            sb.AppendLine("<div class=\"slide\"><div class=\"slide-bg-shape\"></div>");
            sb.AppendLine(BuildSlideHeader("06", "CHIẾN LƯỢC & HÀNH ĐỘNG"));
            sb.AppendLine("<div class=\"slide-content\">");
            
            sb.AppendLine("<div class=\"slide-col-left\">");
            sb.AppendLine("<h2 class=\"section-title\">Chiến lược tiếp thị đề xuất</h2>");
            sb.AppendLine("<div class=\"ai-box\" style=\"border-left-color: #3b82f6;\"><ul class=\"insight-list\">");
            foreach (var strategy in aiInsights?.MarketingStrategies ?? new List<string>()) sb.AppendLine($"<li>{EscapeHtml(strategy)}</li>");
            sb.AppendLine("</ul></div></div>");
            
            sb.AppendLine("<div class=\"slide-col-right\">");
            sb.AppendLine("<h2 class=\"section-title\" style=\"color: #10b981;\">Gợi ý hành động (Action Items)</h2>");
            sb.AppendLine("<div class=\"ai-box\" style=\"border-left-color: #10b981; background: #ecfdf5;\"><ul class=\"insight-list\">");
            foreach (var action in actionItems) sb.AppendLine($"<li>{EscapeHtml(action)}</li>");
            sb.AppendLine("</ul></div></div>");
            
            sb.AppendLine("</div></div>");
        }

        // THANK YOU SLIDE
        sb.AppendLine(@"<div class=""slide"" style=""display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center; position: relative; overflow: hidden; background: linear-gradient(135deg, #f4f8fc 0%, #ffffff 100%);"">
            <div style=""position: absolute; width: 1200px; height: 1200px; border-radius: 50%; background: radial-gradient(circle, rgba(59,130,246,0.04) 0%, rgba(255,255,255,0) 70%); top: -100px; left: -200px; z-index: 0;""></div>
            <div style=""position: absolute; width: 1600px; height: 1600px; border-radius: 50%; background: radial-gradient(circle, rgba(59,130,246,0.03) 0%, rgba(255,255,255,0) 70%); top: -300px; right: -600px; z-index: 0;""></div>
            <div style=""z-index: 1;"">
                <div style=""font-size: 65px; font-weight: 600; color: #475569; margin-bottom: 25px; display: flex; align-items: center; justify-content: center; gap: 15px;"">
                    <svg width=""70"" height=""70"" viewBox=""0 0 100 100"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
                        <circle cx=""50"" cy=""50"" r=""25"" stroke=""#3b82f6"" stroke-width=""6""/>
                        <circle cx=""50"" cy=""50"" r=""12"" fill=""#3b82f6""/>
                        <circle cx=""20"" cy=""50"" r=""6"" fill=""#0ea5e9""/>
                        <circle cx=""80"" cy=""50"" r=""6"" fill=""#0ea5e9""/>
                        <circle cx=""50"" cy=""20"" r=""6"" fill=""#0ea5e9""/>
                        <circle cx=""50"" cy=""80"" r=""6"" fill=""#0ea5e9""/>
                        <circle cx=""29"" cy=""29"" r=""5"" fill=""#0ea5e9""/>
                        <circle cx=""71"" cy=""29"" r=""5"" fill=""#0ea5e9""/>
                        <circle cx=""29"" cy=""71"" r=""5"" fill=""#0ea5e9""/>
                        <circle cx=""71"" cy=""71"" r=""5"" fill=""#0ea5e9""/>
                    </svg>
                    MCFH
                </div>
                <div style=""font-size: 110px; font-weight: 800; color: #1d4ed8; margin-bottom: 45px;"">Xin cảm ơn!</div>
                <div style=""font-size: 24px; color: #1e40af; margin-bottom: 15px; display: flex; align-items: center; justify-content: center; gap: 15px;"">
                    <div style=""width: 10px; height: 10px; background: #93c5fd; border-radius: 50%;""></div>
                    Tăng trưởng Thương hiệu bền vững cùng MCFH
                </div>
                <div style=""font-size: 24px; color: #1e40af; margin-bottom: 80px; display: flex; align-items: center; justify-content: center; gap: 15px;"">
                    <div style=""width: 10px; height: 10px; background: #93c5fd; border-radius: 50%;""></div>
                    Phát hiện sớm cơ hội - Ứng phó kịp thời rủi ro - Tối ưu hiệu quả truyền thông
                </div>
                <div style=""font-size: 22px; color: #1e40af; display: flex; justify-content: center; gap: 60px; font-weight: 500;"">
                    <span><span style=""color: #3b82f6; font-style: italic;"">(Website)</span> mcfh.vn</span>
                    <span><span style=""color: #3b82f6; font-style: italic;"">(Email)</span> info@mcfh.vn</span>
                    <span><span style=""color: #3b82f6; font-style: italic;"">(Phone)</span> 098.130.6460 (Kinh doanh MCFH)</span>
                </div>
            </div>
        </div>");

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
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Width = "1920px", Height = "1080px",
            PrintBackground = true,
            Margin = new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });
        return (pdfBytes, "pdf", rowCount);
    }

    private async Task<(byte[] Content, string Extension, int RowCount)> BuildBespokeSlidePdfAsync(
        int workspaceId, int projectId, int userId, string projectName, MentionQueryDto? filter,
        string? keyword, string? dateFrom, string? dateTo)
    {
        var (html, _, rowCount) = await BuildBespokeSlideHtmlAsync(
            workspaceId, projectId, userId, projectName, filter, keyword, dateFrom, dateTo);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Width = "13.333in",
            Height = "7.5in",
            PrintBackground = true,
            PreferCSSPageSize = false,
            Margin = new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });
        return (pdfBytes, "pdf", rowCount);
    }

    /// <summary>HTML deck 16:9 — Tổng quan / Phân tích / Khuyến nghị (chỉ bespoke).</summary>
    private async Task<(string Content, string Extension, int RowCount)> BuildBespokeSlideHtmlAsync(
        int workspaceId, int projectId, int userId, string projectName, MentionQueryDto? filter,
        string? keyword, string? dateFrom, string? dateTo)
    {
        ProjectOverviewDto? overview;
        SentimentSummaryDto? sentiment;
        ChannelComparisonDto? channels;
        InfluencerAnalyticsDto? influencers;
        List<MentionDto> mentions;

        if (filter != null)
        {
            mentions = await _analytics.GetMentionsAsync(workspaceId, projectId, userId, filter);
            overview = BuildOverviewFromMentions(projectId, projectName, mentions);
            sentiment = BuildSentimentFromMentions(mentions);
            channels = BuildChannelsFromMentions(mentions);
            influencers = BuildInfluencersFromMentions(mentions);
        }
        else
        {
            overview = await _analytics.GetOverviewAsync(workspaceId, projectId, userId);
            sentiment = await _analytics.GetSentimentSummaryAsync(workspaceId, projectId, userId);
            channels = await _analytics.GetChannelComparisonAsync(workspaceId, projectId, userId);
            influencers = await _analytics.GetInfluencersAsync(workspaceId, projectId, userId);
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
        var topChannelInfo = topChannel != null
            ? $"{topChannel.Label} ({topChannel.MentionShare:0.#}% SOV, {topChannel.TotalComments} comments)"
            : "Không có dữ liệu";

        var bespokePinnedQuotes = mentions
            .Where(m => m.PinnedForReport)
            .Select(m => m.Content ?? "")
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Take(5)
            .ToList();

        var aiInsights = await _aiSentiment.GenerateReportInsightsAsync(
            projectName, totalMentions, nsrScore, topChannelInfo, "Không có", bespokePinnedQuotes);

        var executiveInsights = aiInsights?.ExecutiveInsights?.Count > 0
            ? aiInsights.ExecutiveInsights
            : BuildExecutiveInsights(
                totalMentions, totalComments, pendingCount, coverage, dominantSentiment,
                topChannel, topRiskChannel, topInfluencer, null);

        var actionItems = aiInsights?.ActionItems?.Count > 0
            ? aiInsights.ActionItems
            : BuildActionItems(pendingCount, topRiskChannel, topInfluencer, null);

        if (actionItems.Count == 0)
            actionItems.Add("Tiếp tục theo dõi mentions mới và cập nhật phân tích khi có thêm dữ liệu.");

        // Đủ 3 ô khuyến nghị để slide không nhìn trống.
        if (actionItems.Count < 3 && topChannel != null)
            actionItems.Add($"Tăng cường theo dõi {topChannel.Label} (đang dẫn SOV {topChannel.MentionShare:0.#}%) và so sánh NSR với các kênh còn lại.");
        if (actionItems.Count < 3)
            actionItems.Add("Lọc và đọc các mentions có nhiều bình luận nhất để nắm insight định tính ngoài chỉ số tổng hợp.");
        if (actionItems.Count < 3)
            actionItems.Add("Lặp lại báo cáo định kỳ để so sánh biến động sentiment và share of voice theo thời gian.");

        var mentionHighlights = mentions
            .OrderByDescending(m => string.Equals(m.Sentiment, "negative", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.CommentsCount)
            .ThenByDescending(m => m.PostedAt ?? m.ScrapedAt ?? DateTime.MinValue)
            .Take(4)
            .ToList();

        var periodParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(dateFrom) || !string.IsNullOrWhiteSpace(dateTo))
            periodParts.Add($"{dateFrom ?? "…"} → {dateTo ?? "…"}");
        if (!string.IsNullOrWhiteSpace(keyword))
            periodParts.Add($"Keyword: {keyword.Trim()}");
        var periodLabel = periodParts.Count > 0 ? string.Join(" · ", periodParts) : "Phạm vi dữ liệu đã thu thập";

        var theme = ResolveBespokeTheme(keyword, projectName);
        var channelList = channels?.Channels.Take(5).ToList() ?? new List<ChannelStatsDto>();
        var actionTake = actionItems.Take(3).ToList();
        while (actionTake.Count < 3)
            actionTake.Add("Tiếp tục theo dõi dữ liệu định kỳ và cập nhật báo cáo khi có mentions mới.");

        var posPct = sentiment?.PositivePercent ?? 0;
        var negPct = sentiment?.NegativePercent ?? 0;
        var neuPct = sentiment?.NeutralPercent ?? 0;
        var unPct = totalMentions > 0 && sentiment != null
            ? Math.Round(sentiment.Unanalyzed * 100.0 / totalMentions, 1)
            : 0;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Báo cáo chuyên sâu — {EscapeHtml(projectName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@500;600;700;800&family=Open+Sans:wght@400;600&display=swap');");
        sb.AppendLine("@page{size:13.333in 7.5in;margin:0;}");
        sb.AppendLine($":root{{--brand:{theme.Primary};--brand-dark:{theme.PrimaryDark};--brand-soft:{theme.PrimarySoft};--ink:#1f2937;--muted:#6b7280;--paper:#f4f6fb;--chart2:{theme.Chart2};--chart3:{theme.Chart3};}}");
        sb.AppendLine("*{box-sizing:border-box;} html,body{margin:0;padding:0;background:#fff;color:var(--ink);");
        sb.AppendLine("font-family:'Open Sans','Segoe UI',system-ui,sans-serif;-webkit-print-color-adjust:exact;print-color-adjust:exact;}");
        sb.AppendLine("h1,h2,.display,.eyebrow,.section-kicker{font-family:Montserrat,'Segoe UI',sans-serif;}");
        sb.AppendLine(".slide{width:13.333in;height:7.5in;page-break-after:always;break-after:page;position:relative;overflow:hidden;display:block;}");
        sb.AppendLine(".slide:last-child{page-break-after:auto;break-after:auto;}");
        sb.AppendLine(".bokeh{background:radial-gradient(circle at 18% 12%,rgba(255,255,255,.95) 0 48px,transparent 70px),");
        sb.AppendLine("radial-gradient(circle at 72% 18%,rgba(255,255,255,.7) 0 36px,transparent 58px),");
        sb.AppendLine("radial-gradient(circle at 88% 70%,rgba(255,255,255,.55) 0 28px,transparent 50px),");
        sb.AppendLine("linear-gradient(180deg,#f7f8fc 0%,#eef1f8 100%);}");
        sb.AppendLine(".pill-l,.pill-r{position:absolute;width:42px;height:220px;background:var(--brand);border-radius:999px;top:50%;transform:translateY(-50%);}");
        sb.AppendLine(".pill-l{left:-21px;} .pill-r{right:-21px;}");
        sb.AppendLine(".cover{text-align:center;padding:2.1in 1.4in 1in;}");
        sb.AppendLine(".cover .kicker{font-size:22px;font-weight:700;letter-spacing:.18em;text-transform:uppercase;color:#374151;margin:0 0 18px;}");
        sb.AppendLine(".cover .display{font-size:58px;font-weight:800;letter-spacing:.04em;text-transform:uppercase;color:var(--brand);margin:0 0 22px;line-height:1.05;}");
        sb.AppendLine(".cover .sub{max-width:8.2in;margin:0 auto;font-size:15px;line-height:1.65;color:var(--muted);}");
        sb.AppendLine(".cover .meta{margin-top:34px;font-size:13px;color:#4b5563;}");
        sb.AppendLine(".theme-chip{display:inline-block;margin-top:18px;padding:7px 14px;border-radius:999px;background:var(--brand-soft);color:var(--brand-dark);font-size:12px;font-weight:700;letter-spacing:.04em;}");
        sb.AppendLine(".split{display:grid;grid-template-columns:4.4in 1fr;height:100%;}");
        sb.AppendLine(".split-blue{background:var(--brand);color:#fff;padding:0.7in 0.55in;}");
        sb.AppendLine(".split-blue h1{font-size:34px;font-weight:800;text-transform:uppercase;margin:0 0 22px;line-height:1.15;letter-spacing:.02em;}");
        sb.AppendLine(".split-blue p{font-size:14px;line-height:1.7;margin:0 0 16px;color:rgba(255,255,255,.92);}");
        sb.AppendLine(".split-main{padding:0.65in 0.7in;position:relative;}");
        sb.AppendLine(".visual-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:0.4in;}");
        sb.AppendLine(".visual-card{border:5px solid var(--brand);border-radius:22px;background:#111827;color:#fff;min-height:3.6in;padding:22px;display:block;}");
        sb.AppendLine(".visual-card .big{font-size:42px;font-weight:800;margin:18px 0 8px;font-family:Montserrat,sans-serif;}");
        sb.AppendLine(".visual-card .lbl{font-size:12px;letter-spacing:.08em;text-transform:uppercase;opacity:.8;}");
        sb.AppendLine(".visual-card .note{font-size:13px;line-height:1.5;opacity:.85;}");
        sb.AppendLine(".offer{display:grid;grid-template-columns:1fr 4.3in;height:100%;}");
        sb.AppendLine(".offer-left{padding:0.7in 0.8in 0.7in 0.9in;position:relative;}");
        sb.AppendLine(".offer-right{background:var(--brand);color:#fff;padding:0.7in 0.55in;}");
        sb.AppendLine(".offer-right h1{font-size:34px;font-weight:800;text-transform:uppercase;margin:0 0 8px;line-height:1.1;}");
        sb.AppendLine(".offer-right .subh{font-size:28px;font-weight:700;text-transform:uppercase;margin:0 0 28px;opacity:.95;}");
        sb.AppendLine(".offer-row{display:grid;grid-template-columns:1fr 56px 1fr;align-items:center;margin:0 0 28px;min-height:78px;}");
        sb.AppendLine(".offer-left-text{text-align:right;font-size:13px;line-height:1.55;color:#4b5563;padding-right:14px;}");
        sb.AppendLine(".offer-right-text{font-size:13px;line-height:1.55;color:rgba(255,255,255,.95);padding-left:14px;}");
        sb.AppendLine(".num-circle{width:52px;height:52px;border-radius:999px;background:#e8eaf0;color:#111827;display:flex;align-items:center;justify-content:center;");
        sb.AppendLine("font-family:Montserrat,sans-serif;font-size:22px;font-weight:800;margin:0 auto;border:3px solid #fff;box-shadow:0 0 0 2px var(--brand-soft);}");
        sb.AppendLine(".center-pad{padding:0.55in 0.9in;text-align:center;}");
        sb.AppendLine(".center-pad .kicker{font-size:16px;font-weight:700;letter-spacing:.14em;text-transform:uppercase;color:#334155;margin:0 0 8px;}");
        sb.AppendLine(".center-pad .display{font-size:44px;font-weight:800;text-transform:uppercase;color:var(--brand);margin:0 0 14px;}");
        sb.AppendLine(".center-pad .lead{max-width:8.5in;margin:0 auto 28px;font-size:14px;line-height:1.6;color:var(--muted);}");
        sb.AppendLine(".kpi-bar{display:grid;grid-template-columns:repeat(3,1fr);background:var(--brand);border-radius:8px;padding:28px 18px;color:#fff;margin:0 0 22px;}");
        sb.AppendLine(".kpi-bar .item .v{font-size:40px;font-weight:800;font-family:Montserrat,sans-serif;letter-spacing:-.02em;}");
        sb.AppendLine(".kpi-bar .item .l{font-size:12px;opacity:.9;margin-top:6px;letter-spacing:.04em;text-transform:uppercase;}");
        sb.AppendLine(".kpi-desc{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;text-align:center;}");
        sb.AppendLine(".kpi-desc p{margin:0;font-size:13px;line-height:1.55;color:#4b5563;}");
        sb.AppendLine(".pad{padding:0.55in 0.7in;}");
        sb.AppendLine(".title-stack .top{font-size:18px;font-weight:800;letter-spacing:.08em;text-transform:uppercase;color:#111827;margin:0;}");
        sb.AppendLine(".title-stack .bot{font-size:36px;font-weight:800;letter-spacing:.04em;text-transform:uppercase;color:var(--brand);margin:4px 0 16px;}");
        sb.AppendLine(".body-copy{font-size:14px;line-height:1.65;color:#4b5563;margin:0 0 12px;max-width:5.2in;}");
        sb.AppendLine(".accent-bar{width:140px;height:14px;background:var(--brand);margin-top:22px;}");
        sb.AppendLine(".two{display:grid;grid-template-columns:1.05fr 1fr;gap:28px;align-items:start;}");
        sb.AppendLine(".chart-wrap{background:#fff;border:1px solid #e5e7eb;border-radius:18px;padding:18px;}");
        sb.AppendLine(".bar-row{display:grid;grid-template-columns:90px 1fr 54px;gap:10px;align-items:center;margin:0 0 12px;font-size:13px;}");
        sb.AppendLine(".bar-track{height:16px;background:#e8eef8;border-radius:999px;overflow:hidden;}");
        sb.AppendLine(".bar-fill{height:100%;background:linear-gradient(90deg,var(--chart3),var(--brand));border-radius:999px;}");
        sb.AppendLine(".insight-list{margin:0;padding-left:18px;} .insight-list li{margin:0 0 10px;font-size:14px;line-height:1.55;color:#374151;}");
        sb.AppendLine(".foot{position:absolute;left:0.7in;right:0.7in;bottom:0.28in;font-size:11px;color:#9ca3af;display:flex;justify-content:space-between;}");
        sb.AppendLine(".thanks{text-align:center;padding-top:2.2in;} .thanks .display{font-size:54px;font-weight:800;color:var(--brand);text-transform:uppercase;margin:0 0 14px;}");
        sb.AppendLine(".thanks p{font-size:15px;color:var(--muted);line-height:1.6;max-width:8in;margin:0 auto;}");
        sb.AppendLine("</style></head><body>");

        // 1. Cover
        sb.AppendLine("<section class=\"slide bokeh cover\">");
        sb.AppendLine("<div class=\"pill-l\"></div><div class=\"pill-r\"></div>");
        sb.AppendLine("<p class=\"kicker\">MCFH Social Listening</p>");
        sb.AppendLine("<h1 class=\"display\">Báo cáo chuyên sâu</h1>");
        sb.AppendLine($"<p class=\"sub\">{EscapeHtml(projectName)}. Tổng hợp thảo luận online theo ba phần: Tổng quan, Phân tích và Khuyến nghị — dựa trên dữ liệu đã cào của đơn này.</p>");
        sb.AppendLine($"<div class=\"meta\">{EscapeHtml(periodLabel)} · Xuất {generated} · {FormatNumber(totalMentions)} mentions · {FormatNumber(totalComments)} bình luận</div>");
        sb.AppendLine($"<div class=\"theme-chip\">Tone {EscapeHtml(theme.LabelVi)} · {EscapeHtml(theme.Primary)}</div>");
        sb.AppendLine("</section>");

        // 2. Agenda
        sb.AppendLine("<section class=\"slide bokeh\">");
        sb.AppendLine("<div class=\"offer\">");
        sb.AppendLine("<div class=\"offer-left\">");
        sb.AppendLine("<div class=\"pill-l\" style=\"left:-10px;height:160px;\"></div>");
        sb.AppendLine("<div class=\"offer-row\"><div class=\"offer-left-text\">KPI then chốt, sentiment chủ đạo và tóm tắt điều hành để nắm bức tranh nhanh.</div><div class=\"num-circle\">1</div><div></div></div>");
        sb.AppendLine("<div class=\"offer-row\"><div class=\"offer-left-text\">Sentiment, hiệu quả theo kênh, influencer và mentions đáng đọc.</div><div class=\"num-circle\">2</div><div></div></div>");
        sb.AppendLine("<div class=\"offer-row\"><div class=\"offer-left-text\">Hành động ưu tiên cho đội vận hành / truyền thông sau khi đọc báo cáo.</div><div class=\"num-circle\">3</div><div></div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"offer-right\">");
        sb.AppendLine("<h1>Nội dung</h1><div class=\"subh\">Báo cáo</div>");
        sb.AppendLine("<div class=\"offer-row\" style=\"grid-template-columns:1fr;\"><div class=\"offer-right-text\"><strong>Tổng quan</strong><br/>Chỉ số &amp; tóm tắt điều hành</div></div>");
        sb.AppendLine("<div class=\"offer-row\" style=\"grid-template-columns:1fr;\"><div class=\"offer-right-text\"><strong>Phân tích</strong><br/>Sentiment · kênh · creator</div></div>");
        sb.AppendLine("<div class=\"offer-row\" style=\"grid-template-columns:1fr;\"><div class=\"offer-right-text\"><strong>Khuyến nghị</strong><br/>Việc cần làm tiếp theo</div></div>");
        sb.AppendLine("</div></div></section>");

        // 3. KPI bar
        sb.AppendLine("<section class=\"slide bokeh center-pad\">");
        sb.AppendLine("<p class=\"kicker\">Tổng quan dữ liệu</p>");
        sb.AppendLine("<h1 class=\"display\">Chỉ số then chốt</h1>");
        sb.AppendLine($"<p class=\"lead\">Khối lượng thảo luận và chất lượng cảm xúc cho «{EscapeHtml(projectName)}» tại thời điểm xuất báo cáo.</p>");
        sb.AppendLine("<div class=\"kpi-bar\">");
        sb.AppendLine($"<div class=\"item\"><div class=\"v\">{FormatNumber(totalMentions)}</div><div class=\"l\">Mentions</div></div>");
        sb.AppendLine($"<div class=\"item\"><div class=\"v\">{FormatNumber(totalComments)}</div><div class=\"l\">Bình luận</div></div>");
        sb.AppendLine($"<div class=\"item\"><div class=\"v\">{nsrScore:+#.#;-#.#;0}%</div><div class=\"l\">NSR Score</div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"kpi-desc\">");
        sb.AppendLine("<p>Tổng thảo luận đã thu thập trong phạm vi đơn báo cáo.</p>");
        sb.AppendLine("<p>Tổng phản hồi người dùng gắn với các mentions đã cào.</p>");
        sb.AppendLine($"<p>Sentiment chủ đạo: <strong>{EscapeHtml(dominantSentiment)}</strong> · Độ phủ AI {coverage:0.#}%.</p>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class=\"foot\"><span>Tổng quan</span><span>{EscapeHtml(projectName)}</span></div>");
        sb.AppendLine("</section>");

        // 4. Tổng quan split
        sb.AppendLine("<section class=\"slide\">");
        sb.AppendLine("<div class=\"split\">");
        sb.AppendLine("<div class=\"split-blue\">");
        sb.AppendLine("<h1>Tổng quan<br/>điều hành</h1>");
        foreach (var insight in executiveInsights.Take(3))
            sb.AppendLine($"<p>{EscapeHtml(insight)}</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"split-main bokeh\">");
        sb.AppendLine("<div class=\"visual-grid\">");
        sb.AppendLine($"<div class=\"visual-card\"><div class=\"lbl\">Kênh dẫn đầu</div><div class=\"big\">{EscapeHtml(topChannel?.Label ?? "—")}</div><div class=\"note\">{(topChannel != null ? $"{topChannel.MentionShare:0.#}% SOV · {FormatNumber(topChannel.TotalComments)} bình luận" : "Chưa đủ dữ liệu kênh")}</div></div>");
        sb.AppendLine($"<div class=\"visual-card\" style=\"border-color:#111827;\"><div class=\"lbl\">Điểm cần theo dõi</div><div class=\"big\" style=\"font-size:28px;\">{EscapeHtml(topRiskChannel?.Label ?? "Ổn định")}</div><div class=\"note\">{(topRiskChannel != null && topRiskChannel.NegativePercent > 0 ? $"Tiêu cực {topRiskChannel.NegativePercent:0.#}% — ưu tiên đọc mentions kênh này." : "Chưa có kênh rủi ro nổi bật trong tập dữ liệu.")}</div></div>");
        sb.AppendLine("</div></div></div>");
        sb.AppendLine($"<div class=\"foot\"><span>Tổng quan</span><span>{EscapeHtml(theme.LabelVi)}</span></div>");
        sb.AppendLine("</section>");

        // 5. Sentiment charts
        sb.AppendLine("<section class=\"slide bokeh pad\">");
        sb.AppendLine("<div class=\"title-stack\" style=\"text-align:center;\"><p class=\"top\">Phân tích</p><h1 class=\"bot\">Tình hình sentiment</h1></div>");
        sb.AppendLine($"<p class=\"body-copy\" style=\"text-align:center;max-width:8.5in;margin:0 auto 22px;\">Tỷ lệ cảm xúc trên {FormatNumber(totalMentions)} mentions của đơn này.</p>");
        if (sentiment != null && totalMentions > 0)
        {
            sb.AppendLine("<div class=\"two\" style=\"grid-template-columns:1fr 1fr 1.1fr;gap:20px;align-items:center;\">");
            sb.AppendLine("<div class=\"chart-wrap\" style=\"text-align:center;\">");
            sb.AppendLine(BuildDonutSvg(posPct, theme.Chart3, 100 - posPct, "#d1d5db", "Tích cực"));
            sb.AppendLine($"<p style=\"margin:10px 0 0;font-size:13px;color:#4b5563;\"><strong>{FormatNumber(sentiment.Positive)}</strong> tích cực ({posPct:0.#}%)</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"chart-wrap\" style=\"text-align:center;\">");
            sb.AppendLine(BuildGaugeSvg(Math.Clamp((nsrScore + 100) / 2.0, 0, 100), theme.Primary, theme.Chart2));
            sb.AppendLine($"<p style=\"margin:10px 0 0;font-size:13px;color:#4b5563;\">NSR <strong>{nsrScore:+#.#;-#.#;0}%</strong></p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"chart-wrap\" style=\"text-align:center;\">");
            sb.AppendLine(BuildPieSvg(posPct, negPct, neuPct, unPct, theme.Chart3, "#ef4444", theme.Primary, "#9ca3af"));
            sb.AppendLine($"<p style=\"margin:10px 0 0;font-size:12px;color:#4b5563;\">Pos {posPct:0.#}% · Neg {negPct:0.#}% · Neu {neuPct:0.#}%</p>");
            sb.AppendLine("</div></div>");
        }
        else sb.AppendLine("<p class=\"body-copy\">Chưa đủ dữ liệu sentiment.</p>");
        sb.AppendLine($"<div class=\"foot\"><span>Phân tích</span><span>{EscapeHtml(projectName)}</span></div>");
        sb.AppendLine("</section>");

        // 6. Channel bars
        sb.AppendLine("<section class=\"slide bokeh pad\">");
        sb.AppendLine("<div class=\"two\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<div class=\"title-stack\"><p class=\"top\">Phân tích</p><h1 class=\"bot\">Hiệu quả kênh</h1></div>");
        sb.AppendLine("<p class=\"body-copy\">So sánh share of voice và bình luận trên từng nền tảng để chọn kênh ưu tiên theo dõi.</p>");
        if (topChannel != null)
            sb.AppendLine($"<p class=\"body-copy\"><strong>{EscapeHtml(topChannel.Label)}</strong> đang dẫn với {topChannel.MentionShare:0.#}% SOV.</p>");
        sb.AppendLine("<div class=\"accent-bar\"></div>");
        sb.AppendLine("</div><div class=\"chart-wrap\">");
        if (channelList.Count > 0)
        {
            var maxShare = Math.Max(1.0, channelList.Max(c => c.MentionShare));
            foreach (var ch in channelList)
            {
                var w = Math.Max(6, ch.MentionShare / maxShare * 100);
                sb.AppendLine("<div class=\"bar-row\">");
                sb.AppendLine($"<div>{EscapeHtml(ch.Label)}</div>");
                sb.AppendLine($"<div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:{w:0.#}%;\"></div></div>");
                sb.AppendLine($"<div style=\"text-align:right;font-weight:700;\">{ch.MentionShare:0.#}%</div>");
                sb.AppendLine("</div>");
            }
        }
        else sb.AppendLine("<p class=\"body-copy\">Chưa đủ dữ liệu kênh.</p>");
        sb.AppendLine("</div></div>");
        sb.AppendLine($"<div class=\"foot\"><span>Phân tích</span><span>{EscapeHtml(projectName)}</span></div>");
        sb.AppendLine("</section>");

        // 7. Influencer / mentions
        sb.AppendLine("<section class=\"slide bokeh pad\">");
        sb.AppendLine("<div class=\"two\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<div class=\"title-stack\"><p class=\"top\">Phân tích</p><h1 class=\"bot\">Creator &amp; mentions</h1></div>");
        if (topInfluencer != null)
            sb.AppendLine($"<p class=\"body-copy\">Creator nổi bật: <strong>{EscapeHtml(topInfluencer.Name)}</strong> ({EscapeHtml(FormatPlatformLabel(topInfluencer.Platform))}) — score {topInfluencer.InfluenceScore:0.#}, SOV {topInfluencer.ShareOfVoice:0.#}%.</p>");
        else
            sb.AppendLine("<p class=\"body-copy\">Ưu tiên đọc các mentions có nhiều bình luận hoặc dấu hiệu tiêu cực.</p>");
        sb.AppendLine("<div class=\"accent-bar\"></div></div>");
        sb.AppendLine("<div>");
        if (mentionHighlights.Count > 0)
        {
            foreach (var m in mentionHighlights.Take(3))
            {
                sb.AppendLine("<div class=\"chart-wrap\" style=\"margin-bottom:12px;\">");
                sb.AppendLine($"<div style=\"font-weight:700;font-size:14px;\">{EscapeHtml(m.AuthorName ?? "Tác giả không rõ")}</div>");
                sb.AppendLine($"<div style=\"font-size:12px;color:#6b7280;margin:4px 0 8px;\">{EscapeHtml(FormatPlatformLabel(m.Platform))} · {FormatNumber(m.CommentsCount)} bình luận · {EscapeHtml(FormatSentimentLabel(m.Sentiment))}</div>");
                sb.AppendLine($"<div style=\"font-size:13px;line-height:1.5;color:#374151;\">{EscapeHtml(ClipText(m.Content, 140))}</div>");
                sb.AppendLine("</div>");
            }
        }
        else sb.AppendLine("<p class=\"body-copy\">Chưa có mention nổi bật.</p>");
        sb.AppendLine("</div></div>");
        sb.AppendLine($"<div class=\"foot\"><span>Phân tích</span><span>{EscapeHtml(projectName)}</span></div>");
        sb.AppendLine("</section>");

        // 8. Khuyến nghị
        sb.AppendLine("<section class=\"slide bokeh\">");
        sb.AppendLine("<div class=\"offer\">");
        sb.AppendLine("<div class=\"offer-left\">");
        sb.AppendLine("<div class=\"pill-l\" style=\"left:-10px;height:160px;\"></div>");
        for (var i = 0; i < actionTake.Count; i++)
        {
            sb.AppendLine("<div class=\"offer-row\">");
            sb.AppendLine($"<div class=\"offer-left-text\">{EscapeHtml(actionTake[i])}</div>");
            sb.AppendLine($"<div class=\"num-circle\">{i + 1}</div><div></div>");
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"offer-right\">");
        sb.AppendLine("<h1>Khuyến nghị</h1><div class=\"subh\">Ưu tiên</div>");
        sb.AppendLine("<p style=\"font-size:14px;line-height:1.65;color:rgba(255,255,255,.92);margin:0;\">Ba việc nên làm sau khi đọc tổng quan và phân tích — gắn với dữ liệu thực tế của đơn này.</p>");
        sb.AppendLine("</div></div>");
        sb.AppendLine($"<div class=\"foot\" style=\"color:rgba(255,255,255,.55);\"><span>Khuyến nghị</span><span>{EscapeHtml(projectName)}</span></div>");
        sb.AppendLine("</section>");

        // 9. Closing
        sb.AppendLine("<section class=\"slide bokeh thanks\">");
        sb.AppendLine("<div class=\"pill-l\"></div><div class=\"pill-r\"></div>");
        sb.AppendLine("<h1 class=\"display\">Cảm ơn</h1>");
        sb.AppendLine($"<p>Báo cáo chuyên sâu «{EscapeHtml(projectName)}» · Xuất {generated}<br/>Tone {EscapeHtml(theme.LabelVi)} · MCFH Social Listening</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("</body></html>");
        return (sb.ToString(), "html", totalMentions);
    }

    private sealed class BespokeTheme
    {
        public string Key { get; init; } = "general";
        public string LabelVi { get; init; } = "Tổng quát";
        public string Primary { get; init; } = "#1E4BB5";
        public string PrimaryDark { get; init; } = "#163A8C";
        public string PrimarySoft { get; init; } = "#DCE6F8";
        public string Chart2 { get; init; } = "#67B7D1";
        public string Chart3 { get; init; } = "#8EE5EB";
    }

    /// <summary>Chọn palette theo ngữ cảnh keyword/title (công nghệ→xanh, kinh tế→đỏ, …).</summary>
    private static BespokeTheme ResolveBespokeTheme(string? keyword, string? title)
    {
        var text = $"{keyword} {title}".ToLowerInvariant();

        bool Hit(params string[] words) => words.Any(w => text.Contains(w, StringComparison.Ordinal));

        if (Hit("iphone", "samsung", "android", "laptop", "công nghệ", "cong nghe", "tech", "ai ", "ai-", "software",
                "app", "điện thoại", "dien thoai", "gadget", "chip", "semiconductor", "cloud", "startup công nghệ"))
        {
            return new BespokeTheme
            {
                Key = "tech",
                LabelVi = "Công nghệ",
                Primary = "#1E4BB5",
                PrimaryDark = "#163A8C",
                PrimarySoft = "#DCE6F8",
                Chart2 = "#67B7D1",
                Chart3 = "#8EE5EB"
            };
        }

        if (Hit("kinh tế", "kinh te", "economy", "tài chính", "tai chinh", "chứng khoán", "chung khoan", "ngân hàng",
                "ngan hang", "lãi suất", "lai suat", "doanh thu", "profit", "gdp", "đầu tư", "dau tu", "vàng", "vang", "usd"))
        {
            return new BespokeTheme
            {
                Key = "finance",
                LabelVi = "Kinh tế / Tài chính",
                Primary = "#C62828",
                PrimaryDark = "#8E1B1B",
                PrimarySoft = "#F8D7DA",
                Chart2 = "#EF9A9A",
                Chart3 = "#FFCDD2"
            };
        }

        if (Hit("sức khỏe", "suc khoe", "health", "y tế", "y te", "bệnh", "benh", "dược", "duoc", "vaccine", "hospital"))
        {
            return new BespokeTheme
            {
                Key = "health",
                LabelVi = "Sức khỏe",
                Primary = "#0F766E",
                PrimaryDark = "#115E59",
                PrimarySoft = "#CCFBF1",
                Chart2 = "#5EEAD4",
                Chart3 = "#99F6E4"
            };
        }

        if (Hit("làm đẹp", "lam dep", "beauty", "skincare", "mỹ phẩm", "my pham", "cosmetic", "fashion", "thời trang", "thoi trang"))
        {
            return new BespokeTheme
            {
                Key = "beauty",
                LabelVi = "Làm đẹp / Thời trang",
                Primary = "#9D174D",
                PrimaryDark = "#831843",
                PrimarySoft = "#FCE7F3",
                Chart2 = "#F9A8D4",
                Chart3 = "#FBCFE8"
            };
        }

        if (Hit("ẩm thực", "am thuc", "food", "restaurant", "nhà hàng", "nha hang", "đồ ăn", "do an", "coffee", "trà sữa", "tra sua"))
        {
            return new BespokeTheme
            {
                Key = "food",
                LabelVi = "Ẩm thực",
                Primary = "#C2410C",
                PrimaryDark = "#9A3412",
                PrimarySoft = "#FFEDD5",
                Chart2 = "#FB923C",
                Chart3 = "#FDBA74"
            };
        }

        if (Hit("giáo dục", "giao duc", "education", "university", "đại học", "dai hoc", "edtech", "học sinh", "hoc sinh", "sinh viên", "sinh vien"))
        {
            return new BespokeTheme
            {
                Key = "edu",
                LabelVi = "Giáo dục",
                Primary = "#5B21B6",
                PrimaryDark = "#4C1D95",
                PrimarySoft = "#EDE9FE",
                Chart2 = "#A78BFA",
                Chart3 = "#C4B5FD"
            };
        }

        if (Hit("môi trường", "moi truong", "green", "climate", "năng lượng", "nang luong", "ev ", "xe điện", "xe dien"))
        {
            return new BespokeTheme
            {
                Key = "green",
                LabelVi = "Môi trường / Xanh",
                Primary = "#15803D",
                PrimaryDark = "#166534",
                PrimarySoft = "#DCFCE7",
                Chart2 = "#4ADE80",
                Chart3 = "#86EFAC"
            };
        }

        // Fallback: hash ổn định theo keyword để “ngẫu nhiên nhưng khớp đơn”
        var seed = string.IsNullOrWhiteSpace(keyword) ? (title ?? "mcfh") : keyword;
        var paletteIndex = Math.Abs(seed.GetHashCode(StringComparison.OrdinalIgnoreCase)) % 3;
        return paletteIndex switch
        {
            0 => new BespokeTheme
            {
                Key = "general-blue",
                LabelVi = "Tổng quát",
                Primary = "#1E4BB5",
                PrimaryDark = "#163A8C",
                PrimarySoft = "#DCE6F8",
                Chart2 = "#67B7D1",
                Chart3 = "#8EE5EB"
            },
            1 => new BespokeTheme
            {
                Key = "general-slate",
                LabelVi = "Tổng quát",
                Primary = "#334155",
                PrimaryDark = "#1E293B",
                PrimarySoft = "#E2E8F0",
                Chart2 = "#94A3B8",
                Chart3 = "#CBD5E1"
            },
            _ => new BespokeTheme
            {
                Key = "general-indigo",
                LabelVi = "Tổng quát",
                Primary = "#4338CA",
                PrimaryDark = "#3730A3",
                PrimarySoft = "#E0E7FF",
                Chart2 = "#818CF8",
                Chart3 = "#A5B4FC"
            }
        };
    }

    private static string BuildDonutSvg(double valuePct, string fill, double restPct, string restFill, string _)
    {
        valuePct = Math.Clamp(valuePct, 0, 100);
        var r = 54.0;
        var c = 2 * Math.PI * r;
        var dash = c * valuePct / 100.0;
        var gap = c - dash;
        return $"""
            <svg width="160" height="160" viewBox="0 0 160 160">
              <circle cx="80" cy="80" r="{r:0.##}" fill="none" stroke="{restFill}" stroke-width="18"/>
              <circle cx="80" cy="80" r="{r:0.##}" fill="none" stroke="{fill}" stroke-width="18"
                stroke-dasharray="{dash:0.##} {gap:0.##}" stroke-linecap="round" transform="rotate(-90 80 80)"/>
              <text x="80" y="86" text-anchor="middle" font-family="Montserrat,sans-serif" font-size="22" font-weight="800" fill="#111827">{valuePct:0.#}%</text>
            </svg>
            """;
    }

    private static string BuildGaugeSvg(double valuePct, string arc, string track)
    {
        valuePct = Math.Clamp(valuePct, 0, 100);
        var r = 60.0;
        var c = Math.PI * r;
        var dash = c * valuePct / 100.0;
        var gap = c - dash;
        return $"""
            <svg width="180" height="110" viewBox="0 0 180 110">
              <path d="M20 95 A70 70 0 0 1 160 95" fill="none" stroke="{track}" stroke-width="16" stroke-linecap="round"/>
              <path d="M20 95 A70 70 0 0 1 160 95" fill="none" stroke="{arc}" stroke-width="16" stroke-linecap="round"
                stroke-dasharray="{dash:0.##} {gap:0.##}"/>
              <circle cx="90" cy="95" r="6" fill="#111827"/>
            </svg>
            """;
    }

    private static string BuildPieSvg(double a, double b, double c, double d, string ca, string cb, string cc, string cd)
    {
        static (double x, double y) Pt(double ang) =>
            (80 + 60 * Math.Cos(ang), 80 + 60 * Math.Sin(ang));

        var slices = new List<(double pct, string color)>();
        if (a > 0) slices.Add((a, ca));
        if (b > 0) slices.Add((b, cb));
        if (c > 0) slices.Add((c, cc));
        if (d > 0) slices.Add((d, cd));
        if (slices.Count == 0) slices.Add((100, "#d1d5db"));

        var sum = slices.Sum(s => s.pct);
        if (sum <= 0) sum = 100;
        var pie = new StringBuilder();
        pie.Append("""<svg width="160" height="160" viewBox="0 0 160 160">""");
        double angle = -Math.PI / 2;
        foreach (var (pct, color) in slices)
        {
            var sweep = pct / sum * Math.PI * 2;
            if (sweep <= 0) continue;
            var large = sweep > Math.PI ? 1 : 0;
            var start = Pt(angle);
            angle += sweep;
            var end = Pt(angle);
            if (Math.Abs(sweep - Math.PI * 2) < 0.001)
            {
                pie.Append($"""<circle cx="80" cy="80" r="60" fill="{color}"/>""");
            }
            else
            {
                pie.Append(
                    $"""<path d="M80 80 L{start.x:0.##} {start.y:0.##} A60 60 0 {large} 1 {end.x:0.##} {end.y:0.##} Z" fill="{color}"/>""");
            }
        }
        pie.Append("</svg>");
        return pie.ToString();
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
