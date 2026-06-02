using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string? TransactionRef { get; set; }

    public decimal Amount { get; set; }

    public string? Status { get; set; }

    public string? Type { get; set; }

    public int? PlanId { get; set; }

    public int? RequestId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual SubscriptionPlan? Plan { get; set; }

    public virtual BespokeRequest? Request { get; set; }
}
