using System.Globalization;
using System.Text.Json;
using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class SubscriptionService
{
    private readonly McfhDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SubscriptionService(McfhDbContext context) => _context = context;

    public async Task<List<SubscriptionPlanDto>> GetPublicPlansAsync()
    {
        var plans = await _context.SubscriptionPlans.OrderBy(p => p.Price).ToListAsync();
        var subscriberCounts = await _context.Subscriptions
            .Where(s => s.Status == "active")
            .GroupBy(s => s.PlanId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return plans.Select(p => MapPlan(p, subscriberCounts.GetValueOrDefault(p.PlanId))).ToList();
    }

    public async Task<List<SubscriptionPlanDto>> GetAdminPlansAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();
        return await GetPublicPlansAsync();
    }

    public async Task<SubscriptionPlanDto?> UpdatePlanAsync(int adminUserId, int planId, UpdateSubscriptionPlanDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null) return null;

        plan.Name = dto.Name.Trim();
        plan.Price = dto.Price;
        plan.AiCreditLimit = dto.AiCreditLimit;
        await _context.SaveChangesAsync();

        var count = await _context.Subscriptions.CountAsync(s => s.PlanId == planId && s.Status == "active");
        return MapPlan(plan, count);
    }

    public async Task<BillingSummaryDto?> GetBillingSummaryAsync(int userId, int? workspaceId = null)
    {
        var workspace = await ResolveWorkspaceAsync(userId, workspaceId);
        if (workspace == null) return null;

        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.WorkspaceId == workspace.WorkspaceId && s.Status == "active")
            .OrderByDescending(s => s.ExpiryDate)
            .FirstOrDefaultAsync();

        var credits = await _context.WorkspaceCredits
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspace.WorkspaceId);

        var projectCount = await _context.Projects.CountAsync(p =>
            p.WorkspaceId == workspace.WorkspaceId && p.IsDeleted != true);

        var mentionCount = await (
            from f in _context.ScrapedFeedbacks
            join p in _context.Projects on f.ProjectId equals p.ProjectId
            where p.WorkspaceId == workspace.WorkspaceId && f.IsDeleted != true && p.IsDeleted != true
            select f).CountAsync();

        var memberCount = await _context.WorkspaceMembers.CountAsync(m => m.WorkspaceId == workspace.WorkspaceId);

        var plan = subscription?.Plan;
        var limits = ResolveLimits(plan);

        return new BillingSummaryDto
        {
            WorkspaceId = workspace.WorkspaceId,
            WorkspaceName = workspace.Name,
            PlanId = plan?.PlanId,
            PlanName = plan?.Name ?? "Khởi động",
            Status = subscription?.Status ?? "free",
            ExpiryDate = subscription?.ExpiryDate,
            RenewalNote = subscription != null
                ? $"Gia hạn vào ngày {subscription.ExpiryDate:dd/MM/yyyy}"
                : "Bạn đang dùng gói miễn phí — nâng cấp để mở khóa thêm tài nguyên.",
            ProjectUsed = projectCount,
            ProjectLimit = limits.Projects,
            MentionUsed = mentionCount,
            MentionLimit = limits.Mentions,
            MemberUsed = memberCount,
            MemberLimit = limits.Members,
            AiCreditUsed = credits?.UsedCredits ?? 0,
            AiCreditLimit = plan?.AiCreditLimit ?? limits.AiCredits
        };
    }

    public async Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int userId)
    {
        return await _context.Payments
            .Include(p => p.Plan)
            .Where(p => p.CreatedBy == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentHistoryDto
            {
                PaymentId = p.PaymentId,
                TransactionRef = p.TransactionRef,
                Amount = p.Amount,
                AmountLabel = FormatVnd(p.Amount),
                Status = p.Status,
                Type = p.Type,
                PlanName = p.Plan != null ? p.Plan.Name : null,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BillingSummaryDto?> SubscribeAsync(int userId, SubscribeRequestDto dto)
    {
        var workspace = await ResolveWorkspaceAsync(userId, dto.WorkspaceId);
        if (workspace == null) return null;

        var plan = await _context.SubscriptionPlans.FindAsync(dto.PlanId);
        if (plan == null) return null;

        var existing = await _context.Subscriptions
            .Where(s => s.WorkspaceId == workspace.WorkspaceId && s.Status == "active")
            .ToListAsync();
        foreach (var sub in existing)
            sub.Status = "cancelled";

        var now = DateTime.Now;
        _context.Subscriptions.Add(new Subscription
        {
            WorkspaceId = workspace.WorkspaceId,
            PlanId = plan.PlanId,
            StartDate = now,
            ExpiryDate = now.AddMonths(1),
            Status = "active"
        });

        var credits = await _context.WorkspaceCredits
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspace.WorkspaceId);
        if (credits == null)
        {
            credits = new WorkspaceCredit
            {
                WorkspaceId = workspace.WorkspaceId,
                TotalCredits = plan.AiCreditLimit,
                UsedCredits = 0,
                LastUpdated = now
            };
            _context.WorkspaceCredits.Add(credits);
        }
        else
        {
            credits.TotalCredits = plan.AiCreditLimit;
            credits.LastUpdated = now;
        }

        _context.Payments.Add(new Payment
        {
            TransactionRef = $"INV-{now:yyyyMMddHHmmss}",
            Amount = plan.Price,
            Status = "success",
            Type = "subscription",
            PlanId = plan.PlanId,
            CreatedBy = userId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return await GetBillingSummaryAsync(userId, workspace.WorkspaceId);
    }

    private async Task<Workspace?> ResolveWorkspaceAsync(int userId, int? workspaceId)
    {
        if (workspaceId.HasValue)
        {
            var member = await _context.WorkspaceMembers
                .Include(m => m.Workspace)
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
            return member?.Workspace is { IsDeleted: not true } w ? w : null;
        }

        return await _context.WorkspaceMembers
            .Include(m => m.Workspace)
            .Where(m => m.UserId == userId && m.Workspace.IsDeleted != true)
            .OrderBy(m => m.WorkspaceId)
            .Select(m => m.Workspace)
            .FirstOrDefaultAsync();
    }

    private static SubscriptionPlanDto MapPlan(SubscriptionPlan plan, int activeSubscribers)
    {
        var meta = PlanMeta(plan.Name);
        return new SubscriptionPlanDto
        {
            PlanId = plan.PlanId,
            Name = plan.Name,
            Description = meta.Description,
            Price = plan.Price,
            PriceLabel = plan.Price <= 0 ? "Miễn phí" : FormatVnd(plan.Price),
            AiCreditLimit = plan.AiCreditLimit,
            Features = meta.Features,
            ButtonText = meta.ButtonText,
            IsPopular = plan.Name.Contains("Premium", StringComparison.OrdinalIgnoreCase),
            ActiveSubscribers = activeSubscribers
        };
    }

    private static (string Description, List<string> Features, string ButtonText) PlanMeta(string name)
    {
        if (name.Contains("Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "Hệ thống may đo và dịch vụ phân tích dữ liệu từ chuyên gia (Bespoke).",
                new List<string>
                {
                    "Không giới hạn Workspaces",
                    "Dung lượng Mentions theo nhu cầu",
                    "Báo cáo Bespoke chuyên sâu",
                    "Chuyên viên hỗ trợ 1-kèm-1",
                    "SLA & hỗ trợ kỹ thuật 24/7"
                },
                "Nhận báo giá"
            );
        }

        if (name.Contains("Premium", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "Dành cho chiến dịch Marketing cần phân tích dữ liệu chuyên sâu bằng AI.",
                new List<string>
                {
                    "Lên đến 5 Workspaces",
                    "50.000 Mentions/tháng",
                    "AI Sentiment & Aspect",
                    "Theo dõi Influencers",
                    "Xuất báo cáo PDF/CSV"
                },
                "Nâng cấp VIP ngay"
            );
        }

        return (
            "Phù hợp cho cá nhân hoặc doanh nghiệp nhỏ mới làm quen Social Listening.",
            new List<string>
            {
                "1 Workspace",
                "1.000 Mentions/tháng",
                "Dashboard thống kê cơ bản",
                "Lọc từ khóa tiêu chuẩn"
            },
            "Bắt đầu miễn phí"
        );
    }

    private static (int Projects, int Mentions, int Members, int AiCredits) ResolveLimits(SubscriptionPlan? plan)
    {
        if (plan == null) return (1, 1000, 5, 100);
        if (plan.Name.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)) return (999, 500000, 100, plan.AiCreditLimit);
        if (plan.Name.Contains("Premium", StringComparison.OrdinalIgnoreCase)) return (5, 50000, 10, plan.AiCreditLimit);
        return (3, 10000, 5, plan.AiCreditLimit);
    }

    private static string FormatVnd(decimal amount) =>
        string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", amount);

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null && user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
