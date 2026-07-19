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

    /// <summary>PayOS orderCode (duy nhất) — dùng để đối soát webhook / tra cứu payment link.</summary>
    public long? OrderCode { get; set; }

    /// <summary>PayOS paymentLinkId trả về khi tạo checkout.</summary>
    public string? PaymentLinkId { get; set; }

    /// <summary>URL trang thanh toán PayOS (checkoutUrl).</summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>Thời điểm PayOS xác nhận đã thanh toán.</summary>
    public DateTime? PaidAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual SubscriptionPlan? Plan { get; set; }

    public virtual BespokeRequest? Request { get; set; }
}
