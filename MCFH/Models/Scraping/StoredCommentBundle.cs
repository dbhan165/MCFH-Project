namespace MCFH.Models.Scraping;

public class StoredCommentBundle
{
    public List<string> Comments { get; set; } = new();
    public string? AiSummary { get; set; }
    public DateTime? AnalyzedAt { get; set; }
}
