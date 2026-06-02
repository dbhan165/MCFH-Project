using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class WorkspaceCredit
{
    public int WorkspaceId { get; set; }

    public int TotalCredits { get; set; }

    public int? UsedCredits { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
