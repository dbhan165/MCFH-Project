using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Workspace
{
    public int WorkspaceId { get; set; }

    public int OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual WorkspaceCredit? WorkspaceCredit { get; set; }

    public virtual ICollection<WorkspaceInvitation> WorkspaceInvitations { get; set; } = new List<WorkspaceInvitation>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
