namespace MCFH.DTOs.Admin.ProviderKeys;

public class PayOsKeyDto
{
    public int PayOsKeyId { get; set; }

    /// <summary>ClientId không phải secret, có thể hiển thị. Brevo / PayOS đều cho phép.</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>Masked — chỉ vài ký tự.</summary>
    public string ApiKeyMasked { get; set; } = "********";
    public string ChecksumKeyMasked { get; set; } = "********";

    public string Environment { get; set; } = "live";
    public string Status { get; set; } = "active";
    public bool IsDefault { get; set; }
    public string? Note { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreatePayOsKeyDto
{
    public string ClientId { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ChecksumKey { get; set; } = null!;
    public string Environment { get; set; } = "live";
    public bool IsDefault { get; set; }
    public string? Note { get; set; }
}

public class UpdatePayOsKeyDto
{
    public string? ClientId { get; set; }
    public string? ApiKey { get; set; }
    public string? ChecksumKey { get; set; }
    public string? Environment { get; set; }
    public string? Status { get; set; }
    public bool? IsDefault { get; set; }
    public string? Note { get; set; }
}

public class PayOsKeyRevealDto
{
    public int PayOsKeyId { get; set; }
    public string ClientId { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ChecksumKey { get; set; } = null!;
}
