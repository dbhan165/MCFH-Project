namespace MCFH.Models;

/// <summary>
/// Một gói mentions đã mua cho Project thông qua PayOS scrape-order.
/// Mentions quota cộng dồn nếu user mua nhiều gói.
/// Khi status chuyển sang 'exhausted' thì dòng này hết quota nhưng có thể vẫn còn gói khác active.
/// </summary>
public class ProjectMentionPackage
{
    public int PackageId { get; set; }
    public int ProjectId { get; set; }
    public int PaymentId { get; set; }

    /// <summary>"PACK_100" | "PACK_300" | "PACK_600" | "FULL_UNLIMITED".</summary>
    public string PackageType { get; set; } = null!;

    /// <summary>Số mentions được mua (100/300/600) hoặc -1 nếu FULL_UNLIMITED.</summary>
    public int MentionsIncluded { get; set; }

    /// <summary>Số mentions đã dùng (cộng dồn trong package này).</summary>
    public int MentionsUsed { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>"active" | "exhausted" | "expired" | "cancelled".</summary>
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
    public virtual Payment Payment { get; set; } = null!;
}
