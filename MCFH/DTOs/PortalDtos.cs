namespace MCFH.DTOs;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalReporters { get; set; }
    public int TotalClients { get; set; }
    public int TotalWorkspaces { get; set; }
    public int TotalProjects { get; set; }
    public int TotalMentions { get; set; }
    public int PendingBespoke { get; set; }
    public int InProgressBespoke { get; set; }
    public int CompletedBespoke { get; set; }
    public List<AdminRecentBespokeDto> RecentBespoke { get; set; } = new();
    public List<AdminRevenueChartDto> RevenueGrowth { get; set; } = new();
    public List<AdminSubscriptionChartDto> SubscriptionData { get; set; } = new();
    public List<AdminRecentJobDto> RecentJobs { get; set; } = new();
    public List<AdminProxyHealthDto> ProxyHealthOverview { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public double RevenueGrowthRate { get; set; }
    public List<AdminRevenueByTypeDto> RevenueByType { get; set; } = new();
    public List<AdminRevenueByPlanDto> RevenueByPlan { get; set; } = new();
    public List<AdminRecentRevenueTransactionDto> RecentRevenueTransactions { get; set; } = new();
}

public class AdminRevenueByTypeDto
{
    public string Type { get; set; } = null!;
    public string TypeName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double Percentage { get; set; }
    public bool IsTopFeature { get; set; }
}

public class AdminRevenueByPlanDto
{
    public string Name { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}

public class AdminRecentRevenueTransactionDto
{
    public int PaymentId { get; set; }
    public string? TransactionRef { get; set; }
    public string UserName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string FeatureName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? PaidAt { get; set; }
}

public class AdminRevenueChartDto
{
    public string Month { get; set; } = null!;
    public int Revenue { get; set; }
    public int Users { get; set; }
}

public class AdminSubscriptionChartDto
{
    public string Name { get; set; } = null!;
    public int Value { get; set; }
    public string Color { get; set; } = null!;
}

public class AdminRecentJobDto
{
    public string Id { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Progress { get; set; }
}

public class AdminProxyHealthDto
{
    public string Name { get; set; } = null!;
    public double Health { get; set; }
}

public class AdminRecentBespokeDto
{
    public int RequestId { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ClientName { get; set; }
    public string? ReporterName { get; set; }
    public DateTime? Deadline { get; set; }
}

public class AdminUserListDto
{
    public List<AdminUserItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminUserItemDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string SystemRole { get; set; } = null!;
    public bool IsBanned { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class AdminUserDetailDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string AuthProvider { get; set; } = null!;
    public string SystemRole { get; set; } = null!;
    public bool IsBanned { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? BannedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public AdminUserActivityStatsDto Stats { get; set; } = new();
    public List<AdminUserWorkspaceDto> Workspaces { get; set; } = new();
    public List<AdminUserBespokeDto> BespokeRequests { get; set; } = new();
    public List<AdminUserPaymentDto> RecentPayments { get; set; } = new();
}

public class AdminUserActivityStatsDto
{
    public int OwnedWorkspaces { get; set; }
    public int MemberWorkspaces { get; set; }
    public int TotalProjects { get; set; }
    public int BespokeAsClient { get; set; }
    public int BespokeAsReporter { get; set; }
    public int UnreadNotifications { get; set; }
}

public class AdminUserWorkspaceDto
{
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = null!;
    public string MembershipRole { get; set; } = null!;
    public bool IsOwner { get; set; }
    public int ProjectCount { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class AdminUserBespokeDto
{
    public int RequestId { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Involvement { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
}

public class AdminUserPaymentDto
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? PlanName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpdateAdminUserDto
{
    public string? SystemRole { get; set; }
    public bool? IsBanned { get; set; }
}

public class PortalBespokeRequestDto
{
    public int RequestId { get; set; }
    public string Title { get; set; } = null!;
    public string? Requirements { get; set; }
    public string Status { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public DateTime? Deadline { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ClientName { get; set; }
    public string? ReporterName { get; set; }
    public int? ReporterId { get; set; }
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? WorkspaceName { get; set; }
    public List<string> Modules { get; set; } = new();
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public decimal? AgreedPrice { get; set; }
    public bool HasDeliverable { get; set; }
    public int? DeliverableReportId { get; set; }
    public string? RevisionFeedback { get; set; }
    public string? Keyword { get; set; }
    public string? PackageType { get; set; }
}

public class ReporterKanbanDto
{
    public List<PortalBespokeRequestDto> Pending { get; set; } = new();
    public List<PortalBespokeRequestDto> InProgress { get; set; } = new();
    public List<PortalBespokeRequestDto> Completed { get; set; } = new();
}

public class QuoteBespokeDto
{
    public decimal AgreedPrice { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Note { get; set; }
}

public class ReporterPerformanceDto
{
    public int DeliveredCount { get; set; }
    public int InProgressCount { get; set; }
    public int PendingCount { get; set; }
    public double? AvgProcessingDays { get; set; }
    public List<PortalBespokeRequestDto> History { get; set; } = new();
}

public class AssignBespokeGlobalDto
{
    public int ReporterId { get; set; }
}

public class AdminAuditLogDto
{
    public int LogId { get; set; }
    public string Action { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ActorName { get; set; } = null!;
    public string ActorEmail { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Severity { get; set; } = "info";
    public DateTime Timestamp { get; set; }
}
