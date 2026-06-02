using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class BespokeReport
{
    public int ReportId { get; set; }

    public int RequestId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? Version { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual BespokeRequest Request { get; set; } = null!;
}
