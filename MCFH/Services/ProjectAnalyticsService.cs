using System.Text.RegularExpressions;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class ProjectAnalyticsService
{
    private readonly McfhDbContext _context;

    public ProjectAnalyticsService(McfhDbContext context)
    {
        _context = context;
    }

    private async Task<bool> IsMemberAsync(int workspaceId, int userId)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
    }

    private async Task<Project?> GetProjectAsync(int workspaceId, int projectId, int userId)
    {
        if (!await IsMemberAsync(workspaceId, userId)) return null;

        return await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId &&
                                      p.WorkspaceId == workspaceId &&
                                      p.IsDeleted != true);
    }

    public async Task<ProjectOverviewDto?> GetOverviewByProjectIdAsync(int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.IsDeleted != true);
        if (project == null) return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var sentiment = BuildSentimentCounts(feedbacks);

        return new ProjectOverviewDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalMentions = feedbacks.Count,
            TotalComments = feedbacks.Sum(f => f.CommentsCount ?? 0),
            AnalyzedCount = sentiment.Analyzed,
            PendingAnalysisCount = sentiment.Unanalyzed,
            NsrScore = sentiment.NsrScore,
            PositiveCount = sentiment.Positive,
            NegativeCount = sentiment.Negative,
            NeutralCount = sentiment.Neutral,
            PlatformBreakdown = feedbacks
                .GroupBy(f => f.Platform ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<ProjectOverviewDto?> GetOverviewAsync(int workspaceId, int projectId, int userId)
    {
        var project = await GetProjectAsync(workspaceId, projectId, userId);
        if (project == null) return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var sentiment = BuildSentimentCounts(feedbacks);

        return new ProjectOverviewDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalMentions = feedbacks.Count,
            TotalComments = feedbacks.Sum(f => f.CommentsCount ?? 0),
            AnalyzedCount = sentiment.Analyzed,
            PendingAnalysisCount = sentiment.Unanalyzed,
            NsrScore = sentiment.NsrScore,
            PositiveCount = sentiment.Positive,
            NegativeCount = sentiment.Negative,
            NeutralCount = sentiment.Neutral,
            PlatformBreakdown = feedbacks
                .GroupBy(f => f.Platform ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<List<MentionDto>> GetMentionsAsync(
        int workspaceId, int projectId, int userId, MentionQueryDto? filter = null)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return new List<MentionDto>();

        var query = _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Platform) && filter.Platform != "all")
        {
            var platform = filter.Platform.Trim().ToLowerInvariant();
            query = query.Where(f => (f.Platform ?? "").ToLower() == platform);
        }

        if (!string.IsNullOrWhiteSpace(filter?.Sentiment) && filter.Sentiment != "all")
        {
            var sentiment = filter.Sentiment.Trim().ToLowerInvariant();
            if (sentiment == "pending")
                query = query.Where(f => f.AiAnalysis == null || f.AiAnalysis.MainSentiment == null);
            else
                query = query.Where(f =>
                    f.AiAnalysis != null &&
                    f.AiAnalysis.MainSentiment != null &&
                    f.AiAnalysis.MainSentiment.ToLower() == sentiment);
        }

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(f =>
                (f.Content != null && f.Content.ToLower().Contains(search)) ||
                (f.AuthorName != null && f.AuthorName.ToLower().Contains(search)));
        }

        if (filter?.DateFrom.HasValue == true)
            query = query.Where(f => f.ScrapedAt >= filter.DateFrom.Value);

        if (filter?.DateTo.HasValue == true)
            query = query.Where(f => f.ScrapedAt <= filter.DateTo.Value);

        var rows = await query
            .OrderByDescending(f => f.ScrapedAt)
            .Select(f => new
            {
                f.FeedbackId,
                f.AuthorName,
                Platform = f.Platform ?? "unknown",
                f.Content,
                Sentiment = f.AiAnalysis != null ? f.AiAnalysis.MainSentiment : null,
                ConfidenceScore = f.AiAnalysis != null ? f.AiAnalysis.ConfidenceScore : null,
                f.OriginalUrl,
                CommentsCount = f.CommentsCount ?? 0,
                f.ScrapedAt,
                f.CommentsFileUrl
            })
            .ToListAsync();

        var mentions = new List<MentionDto>();
        var repairedIds = new List<int>();

        foreach (var row in rows)
        {
            var bundle = await CommentBundleStorage.LoadAsync(row.FeedbackId, row.CommentsFileUrl);
            var isAnalyzed = row.Sentiment != null;
            var aiSummary = bundle.AiSummary;
            var comments = bundle.Comments;

            // DB lệch file (dữ liệu cũ / file mất / AI ghi summary khi chưa có file)
            if (comments.Count == 0 && row.CommentsCount > 0)
                repairedIds.Add(row.FeedbackId);

            var commentsCount = comments.Count;

            if (string.IsNullOrWhiteSpace(aiSummary) && isAnalyzed)
            {
                aiSummary = bundle.Comments.Count > 0
                    ? BuildFallbackAiSummary(row.Sentiment, bundle.Comments.Count)
                    : $"Phân tích từ nội dung bài: thái độ chủ đạo là {FormatSentimentVi(row.Sentiment)}.";
            }

            mentions.Add(new MentionDto
            {
                FeedbackId = row.FeedbackId,
                AuthorName = row.AuthorName,
                Platform = row.Platform,
                Content = row.Content,
                Sentiment = row.Sentiment,
                ConfidenceScore = row.ConfidenceScore,
                OriginalUrl = row.OriginalUrl,
                CommentsCount = commentsCount,
                ScrapedAt = row.ScrapedAt,
                AiSummary = aiSummary,
                Comments = comments,
                IsAnalyzed = isAnalyzed,
                AnalyzedAt = bundle.AnalyzedAt
            });
        }

        if (repairedIds.Count > 0)
        {
            var toFix = await _context.ScrapedFeedbacks
                .Where(f => repairedIds.Contains(f.FeedbackId))
                .ToListAsync();
            foreach (var f in toFix)
                f.CommentsCount = 0;
            await _context.SaveChangesAsync();
        }

        return mentions;
    }

    public async Task<bool> DeleteMentionAsync(int workspaceId, int projectId, int userId, int feedbackId)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return false;

        var feedback = await _context.ScrapedFeedbacks
            .Include(f => f.AiAnalysis)
            .FirstOrDefaultAsync(f =>
                f.FeedbackId == feedbackId &&
                f.ProjectId == projectId &&
                f.IsDeleted != true);

        if (feedback == null)
            return false;

        if (feedback.AiAnalysis != null)
        {
            var aspects = await _context.FeedbackAspects
                .Where(a => a.AnalysisId == feedback.AiAnalysis.AnalysisId)
                .ToListAsync();
            if (aspects.Count > 0)
                _context.FeedbackAspects.RemoveRange(aspects);

            _context.AiAnalyses.Remove(feedback.AiAnalysis);
        }

        feedback.IsDeleted = true;
        feedback.DeletedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TryDeleteCommentBundle(feedback.FeedbackId);
        return true;
    }

    private static void TryDeleteCommentBundle(int feedbackId)
    {
        try
        {
            var path = CommentBundleStorage.GetBundlePath(feedbackId);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    public async Task<SentimentSummaryDto?> GetSentimentSummaryAsync(int workspaceId, int projectId, int userId)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var counts = BuildSentimentCounts(feedbacks);
        var total = feedbacks.Count;

        return new SentimentSummaryDto
        {
            Total = total,
            Positive = counts.Positive,
            Negative = counts.Negative,
            Neutral = counts.Neutral,
            Unanalyzed = counts.Unanalyzed,
            PositivePercent = total > 0 ? Math.Round(counts.Positive * 100.0 / total, 1) : 0,
            NegativePercent = total > 0 ? Math.Round(counts.Negative * 100.0 / total, 1) : 0,
            NeutralPercent = total > 0 ? Math.Round(counts.Neutral * 100.0 / total, 1) : 0,
            NsrScore = counts.NsrScore
        };
    }

    public async Task<InfluencerAnalyticsDto?> GetInfluencersAsync(int workspaceId, int projectId, int userId)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var dbInfluencers = await _context.Influencers
            .Where(i => i.ProjectId == projectId)
            .ToListAsync();

        var totalMentions = feedbacks.Count;
        var groups = new Dictionary<string, InfluencerAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in feedbacks)
        {
            var platform = (f.Platform ?? "unknown").ToLowerInvariant();
            var name = ResolveAuthorName(f.AuthorName, platform, f.OriginalUrl);
            var key = $"{platform}|{name}";

            if (!groups.TryGetValue(key, out var acc))
            {
                acc = new InfluencerAccumulator
                {
                    Name = name,
                    Platform = platform,
                    HandleUrl = ExtractProfileUrl(platform, f.OriginalUrl)
                };
                groups[key] = acc;
            }

            acc.Mentions++;
            acc.TotalComments += f.CommentsCount ?? 0;

            if (string.IsNullOrWhiteSpace(acc.HandleUrl) && !string.IsNullOrWhiteSpace(f.OriginalUrl))
                acc.HandleUrl = ExtractProfileUrl(platform, f.OriginalUrl);

            var sentiment = f.AiAnalysis?.MainSentiment?.ToLowerInvariant();
            switch (sentiment)
            {
                case "positive": acc.Positive++; break;
                case "negative": acc.Negative++; break;
                case "neutral": acc.Neutral++; break;
            }
        }

        var influencers = groups.Values
            .Select(acc =>
            {
                var dbMatch = dbInfluencers.FirstOrDefault(i =>
                    i.Platform.Equals(acc.Platform, StringComparison.OrdinalIgnoreCase) &&
                    i.Name.Equals(acc.Name, StringComparison.OrdinalIgnoreCase));

                var score = acc.Mentions * 10.0 + acc.TotalComments;
                if (acc.Positive > acc.Negative) score += acc.Positive * 2;
                if (acc.Negative > acc.Positive) score -= acc.Negative;

                return new InfluencerDto
                {
                    Id = $"{acc.Platform}|{acc.Name}",
                    Name = acc.Name,
                    Platform = acc.Platform,
                    HandleUrl = dbMatch?.HandleUrl ?? acc.HandleUrl,
                    Mentions = acc.Mentions,
                    TotalComments = acc.TotalComments,
                    ShareOfVoice = totalMentions > 0
                        ? Math.Round(acc.Mentions * 100.0 / totalMentions, 1)
                        : 0,
                    InfluenceScore = Math.Round(Math.Max(0, score), 1),
                    DominantSentiment = ResolveDominantSentiment(acc.Positive, acc.Negative, acc.Neutral),
                    PositiveCount = acc.Positive,
                    NegativeCount = acc.Negative,
                    NeutralCount = acc.Neutral,
                    Followers = dbMatch?.Followers
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

    public async Task<ChannelComparisonDto?> GetChannelComparisonAsync(int workspaceId, int projectId, int userId)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var totalMentions = feedbacks.Count;
        var totalComments = feedbacks.Sum(f => f.CommentsCount ?? 0);

        var platformOrder = new[] { "facebook", "youtube", "tiktok" };

        var channels = feedbacks
            .GroupBy(f => (f.Platform ?? "unknown").ToLowerInvariant())
            .Select(g =>
            {
                var mentions = g.Count();
                var comments = g.Sum(f => f.CommentsCount ?? 0);
                var positive = 0;
                var negative = 0;
                var neutral = 0;
                var unanalyzed = 0;

                foreach (var f in g)
                {
                    switch (f.AiAnalysis?.MainSentiment?.ToLowerInvariant())
                    {
                        case "positive": positive++; break;
                        case "negative": negative++; break;
                        case "neutral": neutral++; break;
                        default: unanalyzed++; break;
                    }
                }

                var analyzed = positive + negative + neutral;
                var nsr = analyzed > 0 ? Math.Round((positive - negative) * 100.0 / analyzed, 1) : 0;

                return new ChannelStatsDto
                {
                    Platform = g.Key,
                    Label = FormatPlatformLabel(g.Key),
                    Mentions = mentions,
                    MentionShare = totalMentions > 0 ? Math.Round(mentions * 100.0 / totalMentions, 1) : 0,
                    TotalComments = comments,
                    CommentShare = totalComments > 0 ? Math.Round(comments * 100.0 / totalComments, 1) : 0,
                    Positive = positive,
                    Negative = negative,
                    Neutral = neutral,
                    Unanalyzed = unanalyzed,
                    NsrScore = nsr,
                    PositivePercent = analyzed > 0 ? Math.Round(positive * 100.0 / analyzed, 1) : 0,
                    NegativePercent = analyzed > 0 ? Math.Round(negative * 100.0 / analyzed, 1) : 0,
                    NeutralPercent = analyzed > 0 ? Math.Round(neutral * 100.0 / analyzed, 1) : 0
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

    public async Task<AspectAnalysisDto?> GetAspectAnalysisAsync(int workspaceId, int projectId, int userId)
    {
        if (await GetProjectAsync(workspaceId, projectId, userId) == null)
            return null;

        var feedbacks = await _context.ScrapedFeedbacks
            .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
            .Include(f => f.AiAnalysis)
            .ToListAsync();

        var accumulators = AspectTextAnalyzer.Definitions.ToDictionary(
            d => d.Key,
            d => new AspectAccumulator { Key = d.Key, Label = d.Label });

        var analyzedMentions = 0;
        var totalHits = 0;

        foreach (var feedback in feedbacks)
        {
            var bundle = await CommentBundleStorage.LoadAsync(feedback.FeedbackId, feedback.CommentsFileUrl);
            var hits = AspectTextAnalyzer.AnalyzeText(feedback.Content, bundle.Comments);
            if (hits.Count == 0) continue;

            analyzedMentions++;
            foreach (var hit in hits)
            {
                if (!accumulators.TryGetValue(hit.Key, out var acc)) continue;
                acc.TotalMentions++;
                totalHits++;
                switch (hit.Sentiment)
                {
                    case "positive": acc.Positive++; break;
                    case "negative": acc.Negative++; break;
                    default: acc.Neutral++; break;
                }
            }
        }

        var aspects = accumulators.Values
            .Where(a => a.TotalMentions > 0)
            .Select(a => new AspectStatsDto
            {
                Key = a.Key,
                Label = a.Label,
                TotalMentions = a.TotalMentions,
                Positive = a.Positive,
                Negative = a.Negative,
                Neutral = a.Neutral,
                PositivePercent = Math.Round(a.Positive * 100.0 / a.TotalMentions, 1),
                NegativePercent = Math.Round(a.Negative * 100.0 / a.TotalMentions, 1),
                NeutralPercent = Math.Round(a.Neutral * 100.0 / a.TotalMentions, 1)
            })
            .OrderByDescending(a => a.TotalMentions)
            .ToList();

        var topPositive = aspects
            .Where(a => a.Positive > 0)
            .OrderByDescending(a => a.PositivePercent)
            .ThenByDescending(a => a.TotalMentions)
            .FirstOrDefault();

        var topNegative = aspects
            .Where(a => a.Negative > 0)
            .OrderByDescending(a => a.NegativePercent)
            .ThenByDescending(a => a.TotalMentions)
            .FirstOrDefault();

        return new AspectAnalysisDto
        {
            TotalAnalyzedMentions = analyzedMentions,
            TotalAspectHits = totalHits,
            TopPositiveAspect = topPositive != null ? $"{topPositive.Label} ({topPositive.PositivePercent}%)" : null,
            TopNegativeAspect = topNegative != null ? $"{topNegative.Label} ({topNegative.NegativePercent}%)" : null,
            Aspects = aspects
        };
    }

    private sealed class AspectAccumulator
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public int TotalMentions { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
    }

    private static string FormatPlatformLabel(string platform) =>
        platform.ToLowerInvariant() switch
        {
            "facebook" => "Facebook",
            "youtube" => "YouTube",
            "tiktok" => "TikTok",
            _ => char.ToUpper(platform[0]) + platform[1..]
        };

    private sealed class InfluencerAccumulator
    {
        public string Name { get; set; } = "";
        public string Platform { get; set; } = "";
        public string? HandleUrl { get; set; }
        public int Mentions { get; set; }
        public int TotalComments { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
    }

    private static string ResolveAuthorName(string? authorName, string platform, string? originalUrl)
    {
        if (!string.IsNullOrWhiteSpace(authorName))
            return authorName.Trim();

        var handle = ExtractTikTokHandle(originalUrl);
        if (!string.IsNullOrWhiteSpace(handle))
            return handle;

        return platform switch
        {
            "youtube" => "Kênh YouTube",
            "facebook" => "Người đăng Facebook",
            "tiktok" => "Tài khoản TikTok",
            _ => "Không rõ"
        };
    }

    private static string? ExtractTikTokHandle(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = Regex.Match(url, @"tiktok\.com/@([^/?#]+)", RegexOptions.IgnoreCase);
        return match.Success ? $"@{match.Groups[1].Value}" : null;
    }

    private static string? ExtractProfileUrl(string platform, string? originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl)) return null;

        if (platform == "tiktok")
        {
            var handle = ExtractTikTokHandle(originalUrl);
            if (!string.IsNullOrWhiteSpace(handle))
                return $"https://www.tiktok.com/{handle}";
        }

        return originalUrl;
    }

    private static string? ResolveDominantSentiment(int positive, int negative, int neutral)
    {
        if (positive == 0 && negative == 0 && neutral == 0) return null;
        if (positive >= negative && positive >= neutral) return "positive";
        if (negative >= positive && negative >= neutral) return "negative";
        return "neutral";
    }

    private static (int Positive, int Negative, int Neutral, int Unanalyzed, int Analyzed, double NsrScore) BuildSentimentCounts(
        List<ScrapedFeedback> feedbacks)
    {
        var positive = 0;
        var negative = 0;
        var neutral = 0;
        var unanalyzed = 0;

        foreach (var feedback in feedbacks)
        {
            var sentiment = feedback.AiAnalysis?.MainSentiment?.ToLowerInvariant();
            switch (sentiment)
            {
                case "positive":
                    positive++;
                    break;
                case "negative":
                    negative++;
                    break;
                case "neutral":
                    neutral++;
                    break;
                default:
                    unanalyzed++;
                    break;
            }
        }

        var analyzed = positive + negative + neutral;
        var nsr = analyzed > 0
            ? Math.Round((positive - negative) * 100.0 / analyzed, 1)
            : 0;

        return (positive, negative, neutral, unanalyzed, analyzed, nsr);
    }

    private static string BuildFallbackAiSummary(string? sentiment, int commentCount) =>
        $"Phân tích từ {commentCount} bình luận đã gom: thái độ cộng đồng chủ đạo là {FormatSentimentVi(sentiment)}.";

    private static string FormatSentimentVi(string? sentiment) =>
        sentiment?.ToLowerInvariant() switch
        {
            "positive" => "tích cực",
            "negative" => "tiêu cực",
            "neutral" => "trung lập",
            _ => "chưa rõ"
        };
}
