namespace MCFH.Models.Scraping;

public class SearchResult
{
    public string Keyword { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> Urls { get; set; } = new();
}