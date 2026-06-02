using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class WorkspaceMember
{
    public int WorkspaceId { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public virtual WorkspaceRole Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
