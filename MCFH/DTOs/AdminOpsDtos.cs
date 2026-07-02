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
