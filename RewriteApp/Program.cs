using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string filePath = @"d:\MCFH-Project\MCFH\Services\ProjectReportService.cs";
        var lines = new List<string>(File.ReadAllLines(filePath));
        
        string newMethod = """
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
""";
        
        int startIdx = 296; // 0-based index for line 297
        int endIdx = 569;   // 0-based index for line 570
        
        lines.RemoveRange(startIdx, endIdx - startIdx + 1);
        lines.Insert(startIdx, newMethod);
        
        File.WriteAllLines(filePath, lines);
    }
}
