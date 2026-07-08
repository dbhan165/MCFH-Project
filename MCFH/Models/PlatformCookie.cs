namespace MCFH.Models;

public partial class PlatformCookie
{
    public int PlatformCookieId { get; set; }

    public string Platform { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string Status { get; set; } = "active";

    public string? Note { get; set; }

    public int CookieCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? UploadedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
