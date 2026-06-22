// Models/Scraping/ScrapeByKeywordResult.cs
namespace MCFH.Models.Scraping;

public class ScrapeByKeywordResult
{
    public string? Keyword { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<PlatformPostResult> Facebook { get; set; } = new();
    public List<PlatformPostResult> YouTube { get; set; } = new();
    public List<string> TikTok { get; set; } = new(); // Chỉ URL, chưa có comment
}

public class PlatformPostResult
{
    public int FeedbackId { get; set; }
    public string? Author { get; set; }
    public string? Text { get; set; }
    public string Url { get; set; } = "";
    public int CommentsCount { get; set; }
}