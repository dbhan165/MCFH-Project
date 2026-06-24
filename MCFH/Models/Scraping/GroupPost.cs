namespace MCFH.Models.Scraping;

public class GroupPost
{
    public string Author { get; set; } = "";
    public DateTime? PostedAt { get; set; }
    public string Text { get; set; } = "";
    public string PostUrl { get; set; } = "";
    public List<string> Comments { get; set; } = new();
}