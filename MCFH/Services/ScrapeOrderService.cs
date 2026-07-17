using System.Globalization;
using MCFH.Configuration;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Models.Scraping;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MCFH.Services;

public class ScrapeOrderService
{
    private readonly McfhDbContext _context;
    private readonly ScrapeJobRunner _jobRunner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScrapeOptions _scrapeOptions;

    public ScrapeOrderService(
        McfhDbContext context,
        ScrapeJobRunner jobRunner,
        IServiceScopeFactory scopeFactory,
        IOptions<ScrapeOptions> scrapeOptions)
    {
        _context = context;
        _jobRunner = jobRunner;
        _scopeFactory = scopeFactory;
        _scrapeOptions = scrapeOptions.Value;
    }

    public ScrapeQuoteDto GetQuote(int postedSinceDays) => new()
    {
        PostedSinceDays = postedSinceDays,
        TimeRangeLabel = GetTimeRangeLabel(postedSinceDays),
        Price = QuotePrice(postedSinceDays),
        PriceLabel = FormatVnd(QuotePrice(postedSinceDays)),
        EstimatedMinutes = EstimateMinutes(postedSinceDays),
        EstimatedDeliveryLabel = FormatEtaLabel(postedSinceDays)
    };

    public async Task<ScrapeOrderDto?> CreateOrderAsync(int userId, CreateScrapeOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Keyword))
            return null;

        var member = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == dto.WorkspaceId && m.UserId == userId);
        if (!member)
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p =>
                p.ProjectId == dto.ProjectId &&
                p.WorkspaceId == dto.WorkspaceId &&
                p.IsDeleted != true);
        if (project == null)
            return null;

        var price = QuotePrice(dto.PostedSinceDays);
        var now = DateTime.Now;
        var order = new ScrapeOrder
        {
            WorkspaceId = dto.WorkspaceId,
            ProjectId = dto.ProjectId,
            UserId = userId,
            Keyword = dto.Keyword.Trim(),
            PostedSinceDays = dto.PostedSinceDays,
            QuotedPrice = price,
            Status = "quoted",
            ProgressPercent = 0,
            StatusMessage = "Chờ thanh toán để bắt đầu cào dữ liệu.",
            CreatedAt = now
        };

        _context.ScrapeOrders.Add(order);
        await _context.SaveChangesAsync();
        return await MapOrderAsync(order.OrderId, userId);
    }

    public async Task<ScrapeOrderDto?> PayOrderAsync(int userId, int orderId)
    {
        var order = await _context.ScrapeOrders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        if (order == null || order.Status is not ("quoted" or "pending_payment"))
            return null;

        var now = DateTime.Now;
        var payment = new Payment
        {
            TransactionRef = $"SCRAPE-{now:yyyyMMddHHmmss}-{orderId}",
            Amount = order.QuotedPrice,
            Status = "success",
            Type = "scrape_order",
            CreatedBy = userId,
            CreatedAt = now
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var postedDays = order.PostedSinceDays > 0 ? order.PostedSinceDays : (int?)null;
        var jobId = await _jobRunner.StartAsync(order.ProjectId, userId, postedDays);
        if (jobId == null)
            return null;

        order.PaymentId = payment.PaymentId;
        order.ScrapeJobId = jobId;
        order.Status = "scraping";
        order.PaidAt = now;
        order.ProgressPercent = 5;
        order.EstimatedReportAt = now.AddMinutes(EstimateMinutes(order.PostedSinceDays));
        order.StatusMessage = $"Thanh toán thành công. Báo cáo dự kiến sẵn sàng trước {order.EstimatedReportAt:HH:mm dd/MM/yyyy}.";
        await _context.SaveChangesAsync();

        return await MapOrderAsync(orderId, userId);
    }

    public async Task<ScrapeOrderDto?> GetOrderAsync(int userId, int orderId)
    {
        var order = await _context.ScrapeOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        if (order == null)
            return null;

        await SyncOrderProgressAsync(orderId);
        return await MapOrderAsync(orderId, userId);
    }

    public async Task<List<ScrapeOrderDto>> ListOrdersAsync(int userId, int? workspaceId = null, int? projectId = null)
    {
        var query = _context.ScrapeOrders.Where(o => o.UserId == userId);
        if (workspaceId.HasValue)
            query = query.Where(o => o.WorkspaceId == workspaceId);
        if (projectId.HasValue)
            query = query.Where(o => o.ProjectId == projectId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(50).ToListAsync();

        var hadActive = orders.Any(o => o.Status is "scraping" or "analyzing" or "paid");
        foreach (var order in orders.Where(o => o.Status is "scraping" or "analyzing" or "paid"))
            await SyncOrderProgressAsync(order.OrderId);

        if (hadActive)
            orders = await query.OrderByDescending(o => o.CreatedAt).Take(50).ToListAsync();

        var projectIds = orders.Select(o => o.ProjectId).Distinct().ToList();
        var projectNames = await _context.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .ToDictionaryAsync(p => p.ProjectId, p => p.Name);

        return orders.Select(order => MapOrderFromEntity(order, projectNames, userId)).ToList();
    }

    private async Task SyncOrderProgressAsync(int orderId)
    {
        var order = await _context.ScrapeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null)
            return;

        if (order.Status is "completed" or "failed" or "quoted" or "pending_payment")
            return;

        if (order.Status == "scraping" && !string.IsNullOrEmpty(order.ScrapeJobId))
        {
            var job = _jobRunner.GetJob(order.ScrapeJobId, order.UserId);
            if (job == null)
            {
                // Job chỉ tồn tại in-memory — backend restart giữa chừng thì mất.
                // Chuyển sang analyzing để phân tích phần dữ liệu đã cào được.
                order.Status = "analyzing";
                order.ProgressPercent = 85;
                order.StatusMessage = "Hệ thống khởi động lại giữa chừng — đang phân tích phần dữ liệu đã cào được...";
                await _context.SaveChangesAsync();
                _ = RunPostScrapeAsync(orderId);
                return;
            }

            order.ProgressPercent = CalcProgress(job);
            order.StatusMessage = BuildProgressMessage(job) ?? job.PhaseMessage ?? "Đang cào dữ liệu từ các nền tảng...";

            if (job.Status == "failed")
            {
                order.Status = "failed";
                order.StatusMessage = string.IsNullOrWhiteSpace(job.ErrorMessage)
                    ? "Cào dữ liệu thất bại. Vui lòng liên hệ hỗ trợ hoặc thử lại."
                    : $"Cào dữ liệu thất bại: {job.ErrorMessage}";
                await _context.SaveChangesAsync();
            }
            else if (job.Status is "completed" or "cancelled")
            {
                // cancelled: vẫn phân tích phần dữ liệu đã cào được.
                order.Status = "analyzing";
                order.ProgressPercent = 85;
                order.StatusMessage = "Cào xong — AI đang phân tích sentiment và tạo báo cáo...";
                await _context.SaveChangesAsync();
                _ = RunPostScrapeAsync(orderId);
            }
            else
            {
                await _context.SaveChangesAsync();
            }
            return;
        }

        if (order.Status == "analyzing")
        {
            order.ProgressPercent = 85;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>Order đang chạy post-scrape (in-memory) — chặn trigger trùng từ nhiều request poll.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> PostScrapeRunning = new();

    /// <summary>
    /// Hangfire recurring: nhặt lại các order kẹt sau khi backend restart
    /// (job scrape in-memory đã mất, hoặc RunPostScrapeAsync bị ngắt giữa chừng).
    /// </summary>
    public async Task RecoverStuckOrdersAsync()
    {
        var stuck = await _context.ScrapeOrders
            .Where(o => o.Status == "scraping" || o.Status == "analyzing")
            .Select(o => o.OrderId)
            .ToListAsync();

        foreach (var orderId in stuck)
        {
            // SyncOrderProgressAsync tự xử lý cả 2 trường hợp:
            // - scraping + job mất → chuyển analyzing + RunPostScrapeAsync
            // - analyzing → RunPostScrapeAsync có guard, không chạy trùng
            await SyncOrderProgressAsync(orderId);

            var order = await _context.ScrapeOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order?.Status == "analyzing" && !PostScrapeRunning.ContainsKey(orderId))
                _ = RunPostScrapeAsync(orderId);
        }
    }

    private async Task RunPostScrapeAsync(int orderId)
    {
        if (!PostScrapeRunning.TryAdd(orderId, 0))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
            var analyze = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();
            var notify = new NotificationService(db);

            var order = await db.ScrapeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null || order.Status != "analyzing")
                return;

            // Scrape đã tự chạy AI cho feedback mới — ở đây chỉ phân tích phần còn sót,
            // tránh force xóa + chạy lại toàn bộ (tốn quota, chậm gấp đôi).
            var hasPending = await db.ScrapedFeedbacks
                .Where(f => f.ProjectId == order.ProjectId && f.IsDeleted != true)
                .AnyAsync(f => f.AiAnalysis == null);

            AnalyzeProjectResultDto? analyzeResult = null;
            if (hasPending)
                analyzeResult = await analyze.AnalyzePendingFeedbacksAsync(order.ProjectId, false);

            var project = await db.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == order.ProjectId);

            order.Status = "completed";
            order.ProgressPercent = 100;
            order.ReportReadyAt = DateTime.Now;
            order.CompletedAt = DateTime.Now;
            order.StatusMessage = analyzeResult?.Message ?? "Báo cáo đã sẵn sàng.";
            await db.SaveChangesAsync();

            await notify.NotifyAsync(
                order.UserId,
                "Báo cáo cào dữ liệu đã sẵn sàng",
                $"Dự án «{project?.Name ?? order.ProjectId.ToString()}» — từ khóa «{order.Keyword}». {order.StatusMessage}",
                "success",
                "scrape_order",
                order.OrderId,
                order.ProjectId);
        }
        catch
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
            var order = await db.ScrapeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return;
            order.Status = "failed";
            order.StatusMessage = "Phân tích AI gặp lỗi — vui lòng vào dự án và thử «Phân tích lại».";
            order.CompletedAt = DateTime.Now;
            await db.SaveChangesAsync();
        }
        finally
        {
            PostScrapeRunning.TryRemove(orderId, out _);
        }
    }

    private async Task<ScrapeOrderDto?> MapOrderAsync(int orderId, int userId)
    {
        var order = await _context.ScrapeOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        if (order == null)
            return null;

        var projectName = await _context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == order.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync() ?? $"Dự án #{order.ProjectId}";

        return MapOrderFromEntity(order, projectName, userId);
    }

    private ScrapeOrderDto MapOrderFromEntity(
        ScrapeOrder order,
        IReadOnlyDictionary<int, string> projectNames,
        int? userId = null)
    {
        var projectName = projectNames.TryGetValue(order.ProjectId, out var name)
            ? name
            : $"Dự án #{order.ProjectId}";

        return MapOrderFromEntity(order, projectName, userId);
    }

    private ScrapeOrderDto MapOrderFromEntity(ScrapeOrder order, string projectName, int? userId = null)
    {
        ScrapeJobStatusDto? jobDto = null;
        if (userId.HasValue && !string.IsNullOrEmpty(order.ScrapeJobId))
        {
            var job = _jobRunner.GetJob(order.ScrapeJobId, userId.Value);
            jobDto = job?.ToDto();
        }

        return new ScrapeOrderDto
        {
            OrderId = order.OrderId,
            WorkspaceId = order.WorkspaceId,
            ProjectId = order.ProjectId,
            ProjectName = projectName,
            Keyword = order.Keyword,
            PostedSinceDays = order.PostedSinceDays,
            TimeRangeLabel = GetTimeRangeLabel(order.PostedSinceDays),
            QuotedPrice = order.QuotedPrice,
            PriceLabel = FormatVnd(order.QuotedPrice),
            Status = order.Status,
            StatusLabel = GetStatusLabel(order.Status),
            ProgressPercent = order.ProgressPercent,
            StatusMessage = order.StatusMessage,
            ScrapeJobId = order.ScrapeJobId,
            EstimatedReportAt = order.EstimatedReportAt,
            ReportReadyAt = order.ReportReadyAt,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            CompletedAt = order.CompletedAt,
            ScrapeJob = jobDto
        };
    }

    public static decimal QuotePrice(int postedSinceDays) => postedSinceDays switch
    {
        <= 7 => 1_000_000m,
        <= 30 => 1_000_000m,
        <= 90 => 3_000_000m,
        <= 180 => 6_000_000m,
        _ => 12_000_000m
    };

    public static int EstimateMinutes(int postedSinceDays) => postedSinceDays switch
    {
        <= 7 => 20,
        <= 30 => 30,
        <= 90 => 60,
        <= 180 => 120,
        _ => 240
    };

    private int CalcProgress(ScrapeJobState job)
    {
        if (job.Phase == "starting")
            return 5;

        if (job.Phase == "analyzing" && job.Status == "running")
            return 82;

        var platforms = job.Platforms.Values.Where(p => p.Status != "skipped").ToList();
        if (platforms.Count == 0)
            return 8;

        var fbTarget = Math.Max(1, _scrapeOptions.MaxFacebookPosts);
        var videoTarget = Math.Max(1, _scrapeOptions.MaxVideosPerPlatform);
        var newsTarget = Math.Max(1, _scrapeOptions.MaxNewsArticles);

        var sum = platforms.Sum(p => PlatformProgressPercent(p, fbTarget, videoTarget, newsTarget));
        var avg = sum / platforms.Count;

        // Giai đoạn cào chiếm ~8%–78% tổng tiến trình đơn hàng
        return (int)Math.Clamp(8 + avg * 0.70, 8, 78);
    }

    private string? BuildProgressMessage(ScrapeJobState job)
    {
        var platforms = job.Platforms.Values
            .Where(p => p.Status != "skipped")
            .OrderBy(p => PlatformOrder(p.Platform))
            .ToList();
        if (platforms.Count == 0)
            return null;

        var fbTarget = Math.Max(1, _scrapeOptions.MaxFacebookPosts);
        var videoTarget = Math.Max(1, _scrapeOptions.MaxVideosPerPlatform);
        var newsTarget = Math.Max(1, _scrapeOptions.MaxNewsArticles);

        var parts = platforms.Select(p =>
        {
            var label = p.Label switch
            {
                "Facebook" => "FB",
                "YouTube" => "YT",
                "TikTok" => "TT",
                "Tin tức" => "News",
                _ => p.Label
            };
            var target = PlatformTarget(p.Platform, fbTarget, videoTarget, newsTarget);

            return p.Status switch
            {
                "done" => $"{label} {p.Count}/{target} ✓",
                "running" => $"{label} {p.Count}/{target}",
                "error" => $"{label} lỗi",
                _ => $"{label} chờ"
            };
        });

        return string.Join(" · ", parts);
    }

    private static int PlatformProgressPercent(
        Models.Scraping.ScrapePlatformProgressDto platform,
        int fbTarget,
        int videoTarget,
        int newsTarget)
    {
        return platform.Status switch
        {
            "done" => 100,
            "error" => 100,
            "running" => PlatformRunningPercent(platform, fbTarget, videoTarget, newsTarget),
            _ => 0
        };
    }

    private static int PlatformRunningPercent(
        Models.Scraping.ScrapePlatformProgressDto platform,
        int fbTarget,
        int videoTarget,
        int newsTarget)
    {
        var target = PlatformTarget(platform.Platform, fbTarget, videoTarget, newsTarget);
        if (target <= 0)
            return 20;

        // 15% khi vừa bắt đầu + tỷ lệ bài đã lưu / mục tiêu (tối đa 95% trước khi done)
        var ratio = Math.Min(1.0, platform.Count / (double)target);
        return (int)Math.Clamp(15 + ratio * 80, 15, 95);
    }

    private static int PlatformTarget(string platform, int fbTarget, int videoTarget, int newsTarget) =>
        platform.ToLowerInvariant() switch
        {
            "facebook" => fbTarget,
            "news" => newsTarget,
            _ => videoTarget
        };

    private static int PlatformOrder(string platform) => platform.ToLowerInvariant() switch
    {
        "facebook" => 0,
        "youtube" => 1,
        "news" => 2,
        "tiktok" => 3,
        _ => 9
    };

    private static string GetTimeRangeLabel(int days) => days switch
    {
        0 => "Mọi thời gian",
        7 => "1 tuần gần đây",
        30 => "1 tháng gần đây",
        90 => "3 tháng gần đây",
        180 => "6 tháng gần đây",
        365 => "1 năm gần đây",
        _ => $"{days} ngày gần đây"
    };

    private static string FormatEtaLabel(int days)
    {
        var mins = EstimateMinutes(days);
        if (mins < 60)
            return $"Khoảng {mins} phút";
        var hours = mins / 60.0;
        return hours < 2 ? "Khoảng 1–2 giờ" : "Khoảng 2–4 giờ";
    }

    private static string GetStatusLabel(string status) => status switch
    {
        "quoted" => "Chờ thanh toán",
        "pending_payment" => "Chờ thanh toán",
        "paid" => "Đã thanh toán",
        "scraping" => "Đang cào dữ liệu",
        "analyzing" => "Đang phân tích AI",
        "completed" => "Hoàn tất",
        "failed" => "Thất bại",
        _ => status
    };

    private static string FormatVnd(decimal amount) =>
        string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", amount);
}
