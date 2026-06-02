using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class MutedEntity
{
    public int MuteId { get; set; }

    public int ProjectId { get; set; }

    public string EntityType { get; set; } = null!;

    public string EntityValue { get; set; } = null!;

    public int MutedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User MutedByNavigation { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
