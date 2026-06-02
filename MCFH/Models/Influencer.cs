using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Influencer
{
    public int InfluencerId { get; set; }

    public int ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public string? HandleUrl { get; set; }

    public int? Followers { get; set; }

    public double? InfluenceScore { get; set; }

    public int? Reach { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
