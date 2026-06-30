namespace MCFH.DTOs;

public class ScrapeQuoteRequestDto
{
    public int PostedSinceDays { get; set; } = 30;
}

public class ScrapeQuoteDto
{
    public int PostedSinceDays { get; set; }
    public string TimeRangeLabel { get; set; } = null!;
    public decimal Price { get; set; }
    public string PriceLabel { get; set; } = null!;
    public int EstimatedMinutes { get; set; }
    public string EstimatedDeliveryLabel { get; set; } = null!;
}

public class CreateScrapeOrderDto
{
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public string Keyword { get; set; } = null!;
    public int PostedSinceDays { get; set; } = 30;
}

public class ScrapeOrderDto
{
    public int OrderId { get; set; }
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string Keyword { get; set; } = null!;
    public int PostedSinceDays { get; set; }
    public string TimeRangeLabel { get; set; } = null!;
    public decimal QuotedPrice { get; set; }
    public string PriceLabel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public int ProgressPercent { get; set; }
    public string? StatusMessage { get; set; }
    public string? ScrapeJobId { get; set; }
    public DateTime? EstimatedReportAt { get; set; }
    public DateTime? ReportReadyAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Models.Scraping.ScrapeJobStatusDto? ScrapeJob { get; set; }
}
