using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class SavedFilter
{
    public int FilterId { get; set; }

    public int ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public string FilterConfig { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
