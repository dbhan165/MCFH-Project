namespace MCFH.Models;

/// <summary>
/// Một PayOS API key cấu hình cho PayOsService. Multi-key để admin rotate khi key cũ bị revoke.
///
/// Cấu trúc key PayOS gồm 3 thành phần (theo docs PayOS):
///   - ClientId       : "xxx-xxx-xxx-xxx-xxx" từ PayOS Dashboard → Developers → API.
///   - ApiKey         : dùng để ký HMAC + gọi API.
///   - ChecksumKey    : dùng cho webhook signature.
///
/// PayOsService đọc row có <see cref="IsDefault"/> = true để khởi tạo PayOSClient.
/// </summary>
public class PayOsKey
{
    public int PayOsKeyId { get; set; }

    /// <summary>PayOS Client ID (UUID).</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>PayOS API key (encrypted). Dùng ký HMAC cho webhook + gọi REST.</summary>
    public string ApiKeyEncrypted { get; set; } = null!;

    /// <summary>PayOS Checksum key (encrypted). Dùng ký webhook payload.</summary>
    public string ChecksumKeyEncrypted { get; set; } = null!;

    /// <summary>"sandbox" | "live" — chỉ label, không ảnh hưởng logic.</summary>
    public string Environment { get; set; } = "live";

    /// <summary>"active" | "disabled".</summary>
    public string Status { get; set; } = "active";

    public bool IsDefault { get; set; }

    public string? Note { get; set; }

    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
