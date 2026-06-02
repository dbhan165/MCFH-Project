using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class BespokeRequest
{
    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int? ReporterId { get; set; }

    public int? AssignedBy { get; set; }

    public string Title { get; set; } = null!;

    public string? Requirements { get; set; }

    public string? CustomMetrics { get; set; }

    public decimal? AgreedPrice { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? Status { get; set; }

    public virtual User? AssignedByNavigation { get; set; }

    public virtual ICollection<BespokeReport> BespokeReports { get; set; } = new List<BespokeReport>();

    public virtual User Client { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? Reporter { get; set; }
}
