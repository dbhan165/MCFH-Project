using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public int ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Color { get; set; }

    public int CreatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ScrapedFeedback> Feedbacks { get; set; } = new List<ScrapedFeedback>();
}
