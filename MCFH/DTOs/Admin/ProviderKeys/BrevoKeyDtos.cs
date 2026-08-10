namespace MCFH.DTOs.Admin.ProviderKeys;

/// <summary>DTO list — không lộ raw key, chỉ trả masked.</summary>
public class BrevoKeyDto
{
    public int BrevoKeyId { get; set; }
    public string KeyType { get; set; } = "api";
    public string? SmtpLogin { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string Status { get; set; } = "active";
    public bool IsDefault { get; set; }

    /// <summary>Masked API key — chỉ vài ký tự đầu/cuối. Trả về "********" nếu disabled / thiếu.</summary>
    public string ApiKeyMasked { get; set; } = "********";

    public string? Note { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateBrevoKeyDto
{
    /// <summary>"api" (REST) | "smtp" (SMTP login). Validate server-side.</summary>
    public string KeyType { get; set; } = "api";

    /// <summary>Raw API key (hoặc SMTP password nếu KeyType=smtp). Sẽ encrypt trước khi lưu.</summary>
    public string ApiKey { get; set; } = null!;

    public string? SmtpLogin { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public bool IsDefault { get; set; }
    public string? Note { get; set; }
}

public class UpdateBrevoKeyDto
{
    /// <summary>Optional — nếu trống → giữ key cũ.</summary>
    public string? ApiKey { get; set; }

    public string? SmtpLogin { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? Status { get; set; }
    public bool? IsDefault { get; set; }
    public string? Note { get; set; }
}

/// <summary>Trả về key đầy đủ (chỉ dùng khi admin chủ động reveal).</summary>
public class BrevoKeyRevealDto
{
    public int BrevoKeyId { get; set; }
    public string ApiKey { get; set; } = null!;
    public string? SmtpLogin { get; set; }
}
