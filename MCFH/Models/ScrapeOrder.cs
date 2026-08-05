namespace MCFH.Models;

public class ScrapeOrder
{
    public int OrderId { get; set; }
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Keyword { get; set; } = null!;

    /// <summary>Khoảng thời gian cào (để truyền cho runtime scrape). Còn dùng cho order cũ.</summary>
    public int PostedSinceDays { get; set; }

    /// <summary>Gói mentions user đã mua cho order này: PACK_100/PACK_300/PACK_600/FULL_UNLIMITED. Nullable cho order cũ.</summary>
    public string? MentionsPackage { get; set; }

    /// <summary>Snapshot số mentions của package (100/300/600) hoặc null/unlimited. Nullable cho order cũ.</summary>
    public int? MentionsIncluded { get; set; }

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

    /// <summary>FK không ràng buộc tới ScrapePackage.Code (legacy: MentionsPackage lưu code dạng string).</summary>
    public virtual ScrapePackage? Package { get; set; }
}
