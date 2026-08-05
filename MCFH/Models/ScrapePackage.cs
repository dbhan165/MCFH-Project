using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class ScrapePackage
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

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<ScrapeOrder> ScrapeOrders { get; set; } = new List<ScrapeOrder>();
}