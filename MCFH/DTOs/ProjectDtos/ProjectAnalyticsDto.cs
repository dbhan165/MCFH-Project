namespace MCFH.DTOs.ProjectDtos
{
    public class ProjectOverviewDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public int TotalMentions { get; set; }
        public int TotalComments { get; set; }
        public int AnalyzedCount { get; set; }
        public int PendingAnalysisCount { get; set; }
        public double NsrScore { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }
        public int NeutralCount { get; set; }
        public Dictionary<string, int> PlatformBreakdown { get; set; } = new();
    }

    public class MentionDto
    {
        public int FeedbackId { get; set; }
        public string? AuthorName { get; set; }
        public string Platform { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Sentiment { get; set; }
        public double? ConfidenceScore { get; set; }
        public string? OriginalUrl { get; set; }
        public int CommentsCount { get; set; }
        public DateTime? ScrapedAt { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? AiSummary { get; set; }
        public List<string> Comments { get; set; } = new();
        public bool IsAnalyzed { get; set; }
        public DateTime? AnalyzedAt { get; set; }
        public List<MentionTagDto> Tags { get; set; } = new();
        public bool IsSentimentOverridden { get; set; }
        public bool IsCrisisAlert { get; set; }
    }

    public class SentimentSummaryDto
    {
        public int Total { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
        public int Unanalyzed { get; set; }
        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }
        public double NsrScore { get; set; }
    }

    public class InfluencerDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Platform { get; set; } = null!;
        public string? HandleUrl { get; set; }
        public int Mentions { get; set; }
        public int TotalComments { get; set; }
        public double ShareOfVoice { get; set; }
        public double InfluenceScore { get; set; }
        public string? DominantSentiment { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }
        public int NeutralCount { get; set; }
        public int? Followers { get; set; }
    }

    public class InfluencerAnalyticsDto
    {
        public int TotalMentions { get; set; }
        public int UniqueInfluencers { get; set; }
        public List<InfluencerDto> Influencers { get; set; } = new();
    }

    public class ChannelStatsDto
    {
        public string Platform { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int Mentions { get; set; }
        public double MentionShare { get; set; }
        public int TotalComments { get; set; }
        public double CommentShare { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
        public int Unanalyzed { get; set; }
        public double NsrScore { get; set; }
        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }
    }

    public class ChannelComparisonDto
    {
        public int TotalMentions { get; set; }
        public int TotalComments { get; set; }
        public List<ChannelStatsDto> Channels { get; set; } = new();
    }

    public class AspectStatsDto
    {
        public string Key { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int TotalMentions { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Neutral { get; set; }
        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }
    }

    public class AspectAnalysisDto
    {
        public int TotalAnalyzedMentions { get; set; }
        public int TotalAspectHits { get; set; }
        public string? TopPositiveAspect { get; set; }
        public string? TopNegativeAspect { get; set; }
        public List<AspectStatsDto> Aspects { get; set; } = new();
    }
}
