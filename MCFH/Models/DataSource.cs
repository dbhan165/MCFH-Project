using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class DataSource
{
    public int SourceId { get; set; }

    public int ProjectId { get; set; }

    public string Platform { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public string? TargetUrl { get; set; }

    public string? SearchQuery { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<ImportFile> ImportFiles { get; set; } = new List<ImportFile>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ScrapedFeedback> ScrapedFeedbacks { get; set; } = new List<ScrapedFeedback>();

    public virtual ICollection<ScrapingJob> ScrapingJobs { get; set; } = new List<ScrapingJob>();
}
