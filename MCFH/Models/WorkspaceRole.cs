using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class WorkspaceRole
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
