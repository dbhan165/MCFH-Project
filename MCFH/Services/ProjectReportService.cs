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
        return NsrCalculator.CalculateFromMentionDtos(mentions);
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

