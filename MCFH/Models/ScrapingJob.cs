using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class ScrapingJob
{
    public string JobId { get; set; } = null!;

    public int? SourceId { get; set; }

    public int ProjectId { get; set; }

    public int? ProxyId { get; set; }

    public string? Status { get; set; }

    public int? TotalScraped { get; set; }

    public string? ErrorLog { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual SystemProxy? Proxy { get; set; }

    public virtual DataSource? Source { get; set; }
}
