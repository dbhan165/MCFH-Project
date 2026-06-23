namespace MCFH.Models.Scraping;

public class ScrapedComment
{
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public string Source { get; set; } = "youtube";
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}

public class ScrapeResult
{
    public bool Success { get; set; }
    public string? Title { get; set; }
    public int TotalScraped { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ScrapedComment> Comments { get; set; } = new();
}