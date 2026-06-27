namespace MCFH.Models.Scraping;

public class ScrapeJobStartResponse
{
    public string JobId { get; set; } = "";
    public int ProjectId { get; set; }
}

public class ScrapeJobStatusDto
{
    public string JobId { get; set; } = "";
    public int ProjectId { get; set; }
    public string Status { get; set; } = "running";
    public string? Phase { get; set; }
    public string? PhaseMessage { get; set; }
    public List<ScrapePlatformProgressDto> Platforms { get; set; } = new();
    public ScrapeByKeywordResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ScrapePlatformProgressDto
{
    public string Platform { get; set; } = "";
    public string Label { get; set; } = "";
    public string Status { get; set; } = "pending";
    public int Count { get; set; }
    public string? Message { get; set; }
}
