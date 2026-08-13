namespace MCFH.DTOs.ProjectDtos;

public class ReportInsightsResultDto
{
    public List<string> ExecutiveInsights { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public List<string> MarketingStrategies { get; set; } = new();
    public string? NsrComment { get; set; }
    public string? SentimentAnalysis { get; set; }
    public string? ChannelAnalysis { get; set; }
    public string? InfluencerAnalysis { get; set; }
    public SwotDto? SwotAnalysis { get; set; }
}

public class SwotDto
{
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Opportunities { get; set; } = new();
    public List<string> Threats { get; set; } = new();
}
