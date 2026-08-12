namespace MCFH.DTOs;

public class SubscriptionPlanDto
{
    public int PlanId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string PriceLabel { get; set; } = null!;
    public int AiCreditLimit { get; set; }
    public List<string> Features { get; set; } = new();
    public string ButtonText { get; set; } = null!;
    public bool IsPopular { get; set; }
    public int ActiveSubscribers { get; set; }
}

public class BillingSummaryDto
{
    public int? WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = null!;
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = "Khởi động";
    public string Status { get; set; } = "free";
    public DateTime? ExpiryDate { get; set; }
    public string? RenewalNote { get; set; }
    public int ProjectUsed { get; set; }
    public int ProjectLimit { get; set; }
    public int MentionUsed { get; set; }
    public int MentionLimit { get; set; }
    public int MemberUsed { get; set; }
    public int MemberLimit { get; set; }
    public int AiCreditUsed { get; set; }
    public int AiCreditLimit { get; set; }
    public List<ProjectPackageDto> ProjectPackages { get; set; } = new();
}

public class ProjectPackageDto
{
    public int PackageId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string PackageType { get; set; } = null!;
    public int MentionsIncluded { get; set; }
    public int MentionsUsed { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = null!;
}

public class PaymentHistoryDto
{
    public int PaymentId { get; set; }
    public string? TransactionRef { get; set; }
    public decimal Amount { get; set; }
    public string AmountLabel { get; set; } = null!;
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? PlanName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SubscribeRequestDto
{
    public int WorkspaceId { get; set; }
    public int PlanId { get; set; }
}

public class UpdateSubscriptionPlanDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int AiCreditLimit { get; set; }
}
