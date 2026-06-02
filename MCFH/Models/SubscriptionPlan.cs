using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class SubscriptionPlan
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int AiCreditLimit { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
