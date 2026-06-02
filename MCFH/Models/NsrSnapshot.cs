using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class NsrSnapshot
{
    public int SnapshotId { get; set; }

    public int ProjectId { get; set; }

    public string? Platform { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public int? TotalPositive { get; set; }

    public int? TotalNegative { get; set; }

    public int? TotalNeutral { get; set; }

    public int? TotalReach { get; set; }

    public double? NsrScore { get; set; }

    public double? PresenceScore { get; set; }

    public DateTime? CalculatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
