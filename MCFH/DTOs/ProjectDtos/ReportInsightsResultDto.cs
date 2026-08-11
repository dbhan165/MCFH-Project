namespace MCFH.DTOs.ProjectDtos;

public class ReportInsightsResultDto
{
    public List<string> ExecutiveInsights { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public List<string> MarketingStrategies { get; set; } = new();
    public string NsrComment { get; set; } = string.Empty;
    public string SentimentAnalysis { get; set; } = string.Empty;
    public string ChannelAnalysis { get; set; } = string.Empty;
    public string InfluencerAnalysis { get; set; } = string.Empty;
    public SwotAnalysisDto SwotAnalysis { get; set; } = new();
}

public class SwotAnalysisDto
{
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Opportunities { get; set; } = new();
    public List<string> Threats { get; set; } = new();
}
