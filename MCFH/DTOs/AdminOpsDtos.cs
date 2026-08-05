namespace MCFH.DTOs;

public class SystemProxyDto
{
    public int ProxyId { get; set; }
    public string IpAddress { get; set; } = null!;
    public int Port { get; set; }
    public string? AuthUser { get; set; }
    public string? Status { get; set; }
    public int FailCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool Enabled { get; set; }
}

public class UpsertSystemProxyDto
{
    public string IpAddress { get; set; } = null!;
    public int Port { get; set; }
    public string? AuthUser { get; set; }
    public string? AuthPass { get; set; }
    public string? Status { get; set; }
    public bool Enabled { get; set; } = true;
}

public class ScrapingJobDto
{
    public string JobId { get; set; } = null!;
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? SourceId { get; set; }
    public string? Status { get; set; }
    public int TotalScraped { get; set; }
    public string? ErrorLog { get; set; }
    public string? ProxyIp { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class FbSourceDto
{
    public int FbSourceId { get; set; }
    public string GroupUrl { get; set; } = null!;
    public string? GroupName { get; set; }
    public string? Status { get; set; }
    public int AddedBy { get; set; }
    public string? AddedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpsertFbSourceDto
{
    public string GroupUrl { get; set; } = null!;
    public string? GroupName { get; set; }
    public string? Status { get; set; }
    public bool Enabled { get; set; } = true;
}

public class ScrapePackageDto
{
    public int PackageId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int? MaxItems { get; set; }
    public int? MaxSources { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public int ActiveOrdersCount { get; set; }
}

public class UpsertScrapePackageDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int? MaxItems { get; set; }
    public int? MaxSources { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

/// <summary>DTO public cho client — chỉ những gói is_active, không lộ admin fields.</summary>
public class PublicScrapePackageDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public int? MaxItems { get; set; }
    public int? MaxSources { get; set; }
    public int SortOrder { get; set; }
}
