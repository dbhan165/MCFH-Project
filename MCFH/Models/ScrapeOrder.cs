namespace MCFH.Models;

public class ScrapeOrder
{
    public int OrderId { get; set; }
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Keyword { get; set; } = null!;
    public int PostedSinceDays { get; set; }
    public decimal QuotedPrice { get; set; }
    public string Status { get; set; } = "quoted";
    public int? PaymentId { get; set; }
    public string? ScrapeJobId { get; set; }
    public int ProgressPercent { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime? EstimatedReportAt { get; set; }
    public DateTime? ReportReadyAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
    public virtual Project Project { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}
