using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class SystemProxy
{
    public int ProxyId { get; set; }

    public string IpAddress { get; set; } = null!;

    public int Port { get; set; }

    public string? AuthUser { get; set; }

    public string? AuthPass { get; set; }

    public string? Status { get; set; }

    public int? FailCount { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public virtual ICollection<ScrapingJob> ScrapingJobs { get; set; } = new List<ScrapingJob>();
}
