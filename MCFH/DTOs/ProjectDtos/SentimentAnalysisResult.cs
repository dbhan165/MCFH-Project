namespace MCFH.DTOs.ProjectDtos;

public class SentimentAnalysisResult
{
    public string Sentiment { get; set; } = "neutral";
    public double Confidence { get; set; }
    public bool IsCrisisAlert { get; set; }
    public string? Summary { get; set; }
    public bool UsedAiModel { get; set; }
}
