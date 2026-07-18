using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Models.Scraping;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MCFH.Services;

public class AiAnalysisService
{
    private readonly McfhDbContext _context;
    private readonly IAiSentimentService _aiSentimentService;
    private readonly ProjectAlertService _alertService;
    private readonly ILogger<AiAnalysisService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ICommentBundleStorage _bundleStorage;

    private static readonly string[] PositiveWords =
    {
        "tốt", "hay", "thích", "tuyệt", "xuất sắc", "hài lòng", "yêu", "great", "good", "love", "excellent", "amazing"
    };

    private static readonly string[] NegativeWords =
    {
        "tệ", "xấu", "kém", "ghét", "thất vọng", "lỗi", "hỏng", "bad", "hate", "terrible", "awful", "worst"
    };

    public AiAnalysisService(
        McfhDbContext context,
        IAiSentimentService aiSentimentService,
        ProjectAlertService alertService,
        IMemoryCache cache,
        ILogger<AiAnalysisService> logger,
        ICommentBundleStorage bundleStorage)
    {
        _context = context;
        _aiSentimentService = aiSentimentService;
        _alertService = alertService;
        _cache = cache;
        _logger = logger;
        _bundleStorage = bundleStorage;
    }

    public async Task<AnalyzeProjectResultDto?> AnalyzeProjectAsync(
        int workspaceId, int projectId, int userId, bool force = true)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

        if (!isMember) return null;

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId && p.WorkspaceId == workspaceId && p.IsDeleted != true);

        if (!projectExists) return null;

        return await AnalyzePendingFeedbacksAsync(projectId, force);
    }

    /// <summary>Project đang có phiên phân tích chạy — chặn double-run làm hỏng dữ liệu.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> RunningProjects = new();

    public async Task<AnalyzeProjectResultDto> AnalyzePendingFeedbacksAsync(int projectId, bool force = true)
    {
        if (!RunningProjects.TryAdd(projectId, 0))
        {
            return new AnalyzeProjectResultDto
            {
                ProjectId = projectId,
                Message = "Đang có phiên phân tích AI khác chạy cho dự án này — vui lòng đợi hoàn tất."
            };
        }

        var cacheKey = $"Project:{projectId}:AiProgress";
        try
        {
            var feedbacks = await _context.ScrapedFeedbacks
                .Where(f => f.ProjectId == projectId && f.IsDeleted != true)
                .Include(f => f.AiAnalysis)
                .ToListAsync();

            if (feedbacks.Count == 0)
            {
                return new AnalyzeProjectResultDto
                {
                    ProjectId = projectId,
                    Message = "Chưa có mention nào. Hãy cào dữ liệu trước."
                };
            }

            var pendingCount = feedbacks.Count(f => f.AiAnalysis == null);

            // Đã phân tích hết → tự động chạy lại để cập nhật tóm tắt + comment
            if (!force && pendingCount == 0)
                force = true;

            if (force)
                await ClearExistingAnalysesAsync(feedbacks);

            var pending = feedbacks.Where(f => f.AiAnalysis == null).ToList();
            var total = pending.Count;

            _cache.Set(cacheKey, 0, TimeSpan.FromHours(1)); // Initialize progress

            var analyzed = 0;
            var aiModelCount = 0;
            var crisisCountInBatch = 0;

            foreach (var feedback in pending)
            {
                try
                {
                    var comments = await EnsureCommentsAsync(feedback);
                    var analysis = await AnalyzeFeedbackAsync(feedback, comments);

                    if (string.IsNullOrWhiteSpace(analysis.Summary))
                        analysis.Summary = BuildFallbackSummary(feedback.Content, comments.Count, analysis.Sentiment);

                    await _bundleStorage.SaveAiSummaryAsync(
                        feedback.FeedbackId,
                        analysis.Summary,
                        feedback.CommentsFileUrl);

                    if (comments.Count > 0)
                        feedback.CommentsCount = comments.Count;

                    feedback.CommentsFileUrl = CommentBundleStorage.GetRelativeBundlePath(feedback.FeedbackId);

                    _context.AiAnalyses.Add(new AiAnalysis
                    {
                        FeedbackId = feedback.FeedbackId,
                        MainSentiment = analysis.Sentiment,
                        ConfidenceScore = analysis.Confidence,
                        IsCrisisAlert = analysis.IsCrisisAlert,
                        ProcessedAt = DateTime.Now
                    });

                    analyzed++;
                    _cache.Set(cacheKey, total > 0 ? (analyzed * 100 / total) : 100, TimeSpan.FromHours(1));

                    if (analysis.IsCrisisAlert) crisisCountInBatch++;
                    if (analysis.UsedAiModel) aiModelCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phân tích thất bại feedback {FeedbackId}", feedback.FeedbackId);
                }
            }

            if (analyzed > 0)
            {
                await _context.SaveChangesAsync();
                await _alertService.NotifyAfterAnalysisAsync(projectId, crisisCountInBatch);
            }

            var engine = aiModelCount > 0 ? "AI Model" : "rule-based";
            return new AnalyzeProjectResultDto
            {
                ProjectId = projectId,
                AnalyzedCount = analyzed,
                SkippedCount = feedbacks.Count - analyzed,
                TotalFeedbacks = feedbacks.Count,
                Message = analyzed > 0
                    ? $"Đã phân tích {analyzed}/{feedbacks.Count} bài (caption + bình luận) bằng {engine}."
                    : "Không phân tích được bài nào — kiểm tra dữ liệu đã cào hoặc log server."
            };
        }
        finally
        {
            // Luôn dọn progress + nhả lock, kể cả khi exception giữa chừng.
            _cache.Remove(cacheKey);
            RunningProjects.TryRemove(projectId, out _);
        }
    }

    public async Task<AnalyzeProjectResultDto?> AnalyzeSingleFeedbackAsync(
        int workspaceId, int projectId, int userId, int feedbackId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return null;

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId && p.WorkspaceId == workspaceId && p.IsDeleted != true);
        if (!projectExists) return null;

        var feedback = await _context.ScrapedFeedbacks
            .Include(f => f.AiAnalysis)
            .FirstOrDefaultAsync(f =>
                f.FeedbackId == feedbackId &&
                f.ProjectId == projectId &&
                f.IsDeleted != true);

        if (feedback == null) return null;

        await ClearExistingAnalysesAsync(new List<ScrapedFeedback> { feedback });

        try
        {
            var comments = await EnsureCommentsAsync(feedback);
            var analysis = await AnalyzeFeedbackAsync(feedback, comments);

            if (string.IsNullOrWhiteSpace(analysis.Summary))
                analysis.Summary = BuildFallbackSummary(feedback.Content, comments.Count, analysis.Sentiment);

            await _bundleStorage.SaveAiSummaryAsync(
                feedback.FeedbackId,
                analysis.Summary,
                feedback.CommentsFileUrl);

            if (comments.Count > 0)
                feedback.CommentsCount = comments.Count;

            feedback.CommentsFileUrl = CommentBundleStorage.GetRelativeBundlePath(feedback.FeedbackId);

            _context.AiAnalyses.Add(new AiAnalysis
            {
                FeedbackId = feedback.FeedbackId,
                MainSentiment = analysis.Sentiment,
                ConfidenceScore = analysis.Confidence,
                IsCrisisAlert = analysis.IsCrisisAlert,
                ProcessedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _alertService.NotifyAfterAnalysisAsync(projectId, analysis.IsCrisisAlert ? 1 : 0);

            var engine = analysis.UsedAiModel ? "AI Model" : "rule-based";
            return new AnalyzeProjectResultDto
            {
                ProjectId = projectId,
                AnalyzedCount = 1,
                SkippedCount = 0,
                TotalFeedbacks = 1,
                Message = $"Đã phân tích lại mention bằng {engine}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phân tích thất bại feedback {FeedbackId}", feedbackId);
            return new AnalyzeProjectResultDto
            {
                ProjectId = projectId,
                AnalyzedCount = 0,
                SkippedCount = 1,
                TotalFeedbacks = 1,
                Message = "Không phân tích được mention này — kiểm tra dữ liệu hoặc log server."
            };
        }
    }

    private async Task ClearExistingAnalysesAsync(List<ScrapedFeedback> feedbacks)
    {
        var analyses = feedbacks.Where(f => f.AiAnalysis != null).Select(f => f.AiAnalysis!).ToList();
        if (analyses.Count == 0) return;

        var analysisIds = analyses.Select(a => a.AnalysisId).ToList();
        var aspects = await _context.FeedbackAspects
            .Where(a => analysisIds.Contains(a.AnalysisId))
            .ToListAsync();

        if (aspects.Count > 0)
            _context.FeedbackAspects.RemoveRange(aspects);

        _context.AiAnalyses.RemoveRange(analyses);
        await _context.SaveChangesAsync();

        foreach (var f in feedbacks)
            f.AiAnalysis = null;
    }

    private async Task<SentimentAnalysisResult> AnalyzeFeedbackAsync(
        ScrapedFeedback feedback,
        List<string> comments)
    {
        var combinedText = _bundleStorage.BuildCombinedAnalysisText(feedback.Content, comments);

        if (_aiSentimentService.IsConfigured)
        {
            var aiResult = await _aiSentimentService.AnalyzeAsync(
                feedback.Platform ?? "unknown",
                feedback.AuthorName,
                feedback.Content,
                comments,
                combinedText);

            if (aiResult != null && !string.IsNullOrWhiteSpace(aiResult.Summary))
                return aiResult;
        }

        return AnalyzeWithRules(combinedText, comments.Count);
    }

    private static SentimentAnalysisResult AnalyzeWithRules(string? combinedText, int commentCount = 0)
    {
        var sentiment = DetectSentiment(combinedText);
        return new SentimentAnalysisResult
        {
            Sentiment = sentiment,
            Confidence = sentiment == "neutral" ? 0.55 : 0.82,
            IsCrisisAlert = sentiment == "negative",
            Summary = BuildFallbackSummary(combinedText, commentCount, sentiment),
            UsedAiModel = false
        };
    }

    private static string BuildFallbackSummary(string? content, int commentCount, string sentiment)
    {
        var sentimentVi = FormatSentimentVi(sentiment);
        if (commentCount > 0)
            return $"Phân tích từ {commentCount} bình luận đã gom: cộng đồng chủ yếu {sentimentVi}. " +
                   $"Nội dung bài và phản hồi người xem cho thấy thái độ {sentimentVi}.";

        var preview = string.IsNullOrWhiteSpace(content)
            ? "nội dung bài"
            : (content.Length > 120 ? content[..120] + "..." : content);
        return $"Phân tích từ nội dung bài ({preview}): thái độ chủ đạo {sentimentVi}.";
    }

    private static string FormatSentimentVi(string sentiment) =>
        sentiment switch
        {
            "positive" => "tích cực",
            "negative" => "tiêu cực",
            _ => "trung lập"
        };

    private async Task<List<string>> EnsureCommentsAsync(ScrapedFeedback feedback)
    {
        var bundle = await _bundleStorage.LoadAsync(feedback.FeedbackId, feedback.CommentsFileUrl);
        if (bundle.Comments.Count > 0)
            return bundle.Comments;

        // Phân tích AI không mở browser — comment thiếu thì cào lại từ project, không refetch ở đây
        var expected = feedback.CommentsCount ?? 0;
        if (expected > 0)
        {
            _logger.LogWarning(
                "Feedback {FeedbackId}: DB ghi {Count} comment nhưng file trống — bỏ qua refetch khi phân tích.",
                feedback.FeedbackId,
                expected);
        }

        return bundle.Comments;
    }

    private static string DetectSentiment(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "neutral";

        var lower = content.ToLowerInvariant();
        var hasPositive = PositiveWords.Any(word => lower.Contains(word));
        var hasNegative = NegativeWords.Any(word => lower.Contains(word));

        if (hasPositive && !hasNegative) return "positive";
        if (hasNegative && !hasPositive) return "negative";
        if (hasNegative) return "negative";
        if (hasPositive) return "positive";
        return "neutral";
    }
}
