using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Subscription
{
    public int SubscriptionId { get; set; }

    public int WorkspaceId { get; set; }

    public int PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string? Status { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
