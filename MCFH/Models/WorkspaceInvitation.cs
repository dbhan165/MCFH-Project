using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class WorkspaceInvitation
{
    public int InvitationId { get; set; }

    public int WorkspaceId { get; set; }

    public string InvitedEmail { get; set; } = null!;

    public int InvitedBy { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User InvitedByNavigation { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
