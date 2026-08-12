namespace MCFH.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;

public static class NsrCalculator
{
    // Định nghĩa các hằng số trọng số mặc định cho hệ thống (0 appsettings / 0 DB schema change)
    public const double HIGH_ENGAGEMENT_MULTIPLIER = 2.0; // Tương tác cao (KOL/Post HOT) -> x2 điểm
    public const int HIGH_ENGAGEMENT_THRESHOLD = 20;     // Ngưỡng tương tác >= 20
    public const double NEWS_PLATFORM_MULTIPLIER = 1.5;   // Nguồn Báo chí -> x1.5 điểm
    public const double MEDIA_PLATFORM_MULTIPLIER = 1.3;  // Nguồn Mạng xã hội / Truyền thông -> x1.3 điểm
    public const double YOUTUBE_PLATFORM_MULTIPLIER = MEDIA_PLATFORM_MULTIPLIER;
    public const double DEFAULT_WEIGHT = 1.0;

    /// <summary>
    /// Tính trọng số cho từng Mention/Feedback dựa trên số tương tác và nền tảng.
    /// </summary>
    public static double GetMentionWeight(int engagementCount, string? platform)
    {
        double weight = DEFAULT_WEIGHT;

        // 1. Bài viết/Comment có lượt tương tác cao (Lan tỏa rộng / KOL)
        if (engagementCount >= HIGH_ENGAGEMENT_THRESHOLD)
        {
            weight *= HIGH_ENGAGEMENT_MULTIPLIER;
        }

        // 2. Nền tảng báo chí / tin tức hoặc truyền thông & mạng xã hội
        var plat = platform?.ToLowerInvariant();
        if (plat == "news")
        {
            weight *= NEWS_PLATFORM_MULTIPLIER;
        }
        else if (plat == "youtube" || plat == "facebook" || plat == "tiktok" || plat == "threads" || plat == "instagram" || (!string.IsNullOrEmpty(plat) && plat != "default"))
        {
            weight *= MEDIA_PLATFORM_MULTIPLIER;
        }

        return weight;
    }

    /// <summary>
    /// Tính toán điểm Weighted NSR từ danh sách ScrapedFeedback
    /// </summary>
    public static (int Positive, int Negative, int Neutral, int Unanalyzed, int Analyzed, double NsrScore) CalculateFromFeedbacks(
        IEnumerable<ScrapedFeedback> feedbacks)
    {
        int positive = 0;
        int negative = 0;
        int neutral = 0;
        int unanalyzed = 0;

        double weightedPositive = 0;
        double weightedNegative = 0;
        double totalWeight = 0;

        foreach (var feedback in feedbacks)
        {
            var sentiment = feedback.AiAnalysis?.MainSentiment?.ToLowerInvariant();
            int engagement = feedback.EngagementCount ?? feedback.CommentsCount ?? 0;
            double w = GetMentionWeight(engagement, feedback.Platform);

            switch (sentiment)
            {
                case "positive":
                    positive++;
                    weightedPositive += w;
                    totalWeight += w;
                    break;
                case "negative":
                    negative++;
                    weightedNegative += w;
                    totalWeight += w;
                    break;
                case "neutral":
                    neutral++;
                    totalWeight += w;
                    break;
                default:
                    unanalyzed++;
                    break;
            }
        }

        int analyzed = positive + negative + neutral;
        double nsrScore = totalWeight > 0
            ? Math.Round((weightedPositive - weightedNegative) * 100.0 / totalWeight, 1)
            : 0;

        return (positive, negative, neutral, unanalyzed, analyzed, nsrScore);
    }

    /// <summary>
    /// Tính toán điểm Weighted NSR từ danh sách MentionDto
    /// </summary>
    public static (int Positive, int Negative, int Neutral, double NsrScore) CalculateFromMentionDtos(
        IEnumerable<MentionDto> mentions)
    {
        int positive = 0;
        int negative = 0;
        int neutral = 0;

        double weightedPositive = 0;
        double weightedNegative = 0;
        double totalWeight = 0;

        foreach (var m in mentions)
        {
            var sentiment = m.Sentiment?.ToLowerInvariant();
            int engagement = m.CommentsCount;
            double w = GetMentionWeight(engagement, m.Platform);

            switch (sentiment)
            {
                case "positive":
                    positive++;
                    weightedPositive += w;
                    totalWeight += w;
                    break;
                case "negative":
                    negative++;
                    weightedNegative += w;
                    totalWeight += w;
                    break;
                case "neutral":
                    neutral++;
                    totalWeight += w;
                    break;
            }
        }

        int analyzed = positive + negative + neutral;
        double nsrScore = totalWeight > 0
            ? Math.Round((weightedPositive - weightedNegative) * 100.0 / totalWeight, 1)
            : 0;

        return (positive, negative, neutral, nsrScore);
    }
}
