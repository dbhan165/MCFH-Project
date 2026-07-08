namespace MCFH.DTOs;

public class PlatformCookieDto
{
    public int PlatformCookieId { get; set; }
    public string Platform { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Note { get; set; }
    public int CookieCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool FileExists { get; set; }
    public bool FileMissing { get; set; }
    public bool IsExpiringSoon { get; set; }
    public string? BackupFilePath { get; set; }
    public bool BackupExists { get; set; }
    public Dictionary<string, bool>? RequiredCookiesPresent { get; set; }
}

public class UpdatePlatformCookieMetaDto
{
    public string? Status { get; set; }
    public string? Note { get; set; }
    public string? FilePath { get; set; }
}

public class UpdatePlatformCookieContentDto
{
    public string? CookiesJson { get; set; }
}

public class PlatformCookieContentResultDto
{
    public string Message { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public int CookieCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? UploadedAt { get; set; }
    public bool BackupCreated { get; set; }
}
