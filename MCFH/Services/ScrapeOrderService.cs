using System.Globalization;
using MCFH.Configuration;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Models.Scraping;
using MCFH.Services.Payments;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace MCFH.Services;

public class ScrapeOrderService
{
    private readonly McfhDbContext _context;
    private readonly ScrapeJobRunner _jobRunner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScrapeOptions _scrapeOptions;
    private readonly PayOsOptions _payOsOptions;
    private readonly PayOsService _payOs;
    private readonly ScrapePackageCatalog _catalog;
    private readonly ILogger<ScrapeOrderService> _logger;

    public ScrapeOrderService(
        McfhDbContext context,
        ScrapeJobRunner jobRunner,
        IServiceScopeFactory scopeFactory,
        IOptions<ScrapeOptions> scrapeOptions,
        IOptions<PayOsOptions> payOsOptions,
        PayOsService payOs,
        ScrapePackageCatalog catalog,
        ILogger<ScrapeOrderService> logger)
    {
        _context = context;
        _jobRunner = jobRunner;
        _scopeFactory = scopeFactory;
        _scrapeOptions = scrapeOptions.Value;
        _payOsOptions = payOsOptions.Value;
        _payOs = payOs;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<ScrapeQuoteDto?> GetQuoteAsync(string mentionsPackage, Project? project = null)
    {
        var pkg = await _catalog.GetActiveByCodeAsync(mentionsPackage);
        if (pkg == null) return null;

        return new ScrapeQuoteDto
        {
            MentionsPackage = pkg.Code,
            PackageLabel = pkg.Name,
            MentionsIncluded = pkg.MaxItems ?? 0,
            Price = pkg.Price,
            PriceLabel = FormatVnd(pkg.Price),
            EstimatedMinutes = EstimateMinutesByPackage(pkg),
            EstimatedDeliveryLabel = FormatEtaLabelByPackage(pkg),
            ProjectRemainingMentions = project == null ? null : CalcRemainingMentions(project),
            ProjectHasFullUnlimited = project?.MentionsFullUnlimited ?? false
        };
    }

    /// <summary>Tính số mentions còn lại có thể cào (NULL = unlimited).</summary>
    public static int? CalcRemainingMentions(Project p)
    {
        if (p.MentionsFullUnlimited) return null;
        var remaining = p.MentionsQuotaTotal - p.MentionsQuotaUsed;
        return remaining < 0 ? 0 : remaining;
    }

    public async Task<ScrapeOrderDto?> CreateOrderAsync(int userId, CreateScrapeOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Keyword))
        {
            _logger.LogWarning("[CreateOrder] Thiếu keyword.");
            return null;
        }

        // Lookup gói từ catalog — không hardcode enum: chấp nhận mọi code hợp lệ trong SCRAPE_PACKAGES.
        // Trước đây dùng MentionPackageTypes.Normalize chỉ match PACK_100/300/600/FULL_UNLIMITED —
        // nếu admin tạo gói với code khác sẽ bị trả null dù gói đang active.
        var pkg = await _catalog.GetActiveByCodeAsync(dto.MentionsPackage);
        if (pkg == null)
        {
            _logger.LogWarning("[CreateOrder] Gói '{Code}' không tồn tại hoặc không active.", dto.MentionsPackage);
            return null;
        }

        var member = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == dto.WorkspaceId && m.UserId == userId);
        if (!member)
        {
            _logger.LogWarning("[CreateOrder] User {UserId} không phải member của workspace {WorkspaceId}.", userId, dto.WorkspaceId);
            return null;
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p =>
                p.ProjectId == dto.ProjectId &&
                p.WorkspaceId == dto.WorkspaceId &&
                p.IsDeleted != true);
        if (project == null)
        {
            _logger.LogWarning("[CreateOrder] Project {ProjectId} không tồn tại trong workspace {WorkspaceId}.", dto.ProjectId, dto.WorkspaceId);
            return null;
        }

        // Không chặn ở đây: quota sẽ được cộng vào Project SAU khi thanh toán thành công (xem FulfillPaidOrderAsync).
        // CreateOrder chỉ tạo order "quoted"; job cào thật sẽ kiểm quota khi chạy (ScrapeJobRunner / Controller job start).
        // Logic cũ (reject nếu hết quota) chặn cả order đầu tiên của project mới (total=0, used=0 → remaining=0).
        var pkgCodeUpper = pkg.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var isFull = pkgCodeUpper.StartsWith("FULL", StringComparison.OrdinalIgnoreCase);

        var now = DateTime.Now;
        var order = new ScrapeOrder
        {
            WorkspaceId = dto.WorkspaceId,
            ProjectId = dto.ProjectId,
            UserId = userId,
            Keyword = dto.Keyword.Trim(),
            PostedSinceDays = 30, // Hardcode theo contract đã confirm
            MentionsPackage = pkg.Code,
            MentionsIncluded = isFull ? -1 : (pkg.MaxItems ?? 0),
            QuotedPrice = pkg.Price,
            Status = "quoted",
            ProgressPercent = 0,
            StatusMessage = "Chờ thanh toán để bắt đầu cào dữ liệu.",
            CreatedAt = now
        };

        _context.ScrapeOrders.Add(order);
        await _context.SaveChangesAsync();
        return await MapOrderAsync(order.OrderId, userId);
    }

    /// <summary>
    /// Tạo checkout PayOS cho đơn: Payment status "pending", order → "pending_payment".
    /// Frontend redirect người dùng sang CheckoutUrl; job cào CHỈ chạy sau khi webhook/confirm xác thực đã trả tiền.
    /// </summary>
    public async Task<ScrapeOrderCheckoutDto?> PayOrderAsync(int userId, int orderId)
    {
        var order = await _context.ScrapeOrders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        if (order == null || order.Status is not ("quoted" or "pending_payment"))
            return null;

        // Dev hardcode: bỏ qua PayOS — đánh dấu đã trả + khởi động scrape ngay.
        if (_payOsOptions.Bypass)
            return await PayOrderBypassAsync(userId, order);

        // Đã có checkout đang chờ → kiểm tra lại trên PayOS trước khi tạo link mới.
        Payment? payment = null;
        if (order.PaymentId != null)
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == order.PaymentId && p.Status == "pending");
            if (payment?.OrderCode != null)
            {
                var link = await _payOs.GetPaymentLinkAsync(payment.OrderCode.Value);
                if (link?.Status == PaymentLinkStatus.Paid)
                {
                    // Người dùng đã trả nhưng webhook chưa tới — hoàn tất luôn.
                    await FulfillPaidOrderAsync(order, payment);
                    return await BuildCheckoutDtoAsync(order, payment, payment.CheckoutUrl ?? "", "");
                }
                if (link?.Status == PaymentLinkStatus.Pending && !string.IsNullOrEmpty(payment.CheckoutUrl))
                    return await BuildCheckoutDtoAsync(order, payment, payment.CheckoutUrl, "");
                // Không tra cứu được PayOS (null) → giữ link cũ nếu còn checkoutUrl, tránh tạo link trùng.
                if (link == null && !string.IsNullOrEmpty(payment.CheckoutUrl))
                {
                    _logger.LogWarning(
                        "Không tra cứu được PayOS orderCode {OrderCode} — tái sử dụng checkoutUrl hiện có.",
                        payment.OrderCode);
                    return await BuildCheckoutDtoAsync(order, payment, payment.CheckoutUrl, "");
                }
                // Link cũ hết hạn / bị hủy → đánh dấu failed rồi tạo link mới bên dưới.
                if (link?.Status is PaymentLinkStatus.Cancelled or PaymentLinkStatus.Expired or PaymentLinkStatus.Failed)
                {
                    payment.Status = "failed";
                    await _context.SaveChangesAsync();
                    payment = null;
                }
                else if (!string.IsNullOrEmpty(payment.CheckoutUrl))
                {
                    return await BuildCheckoutDtoAsync(order, payment, payment.CheckoutUrl, "");
                }
            }
        }

        var now = DateTime.Now;
        // orderCode PayOS phải là số duy nhất — unix ms + 2 số ngẫu nhiên, vẫn dưới ngưỡng MAX_SAFE_INTEGER.
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100 + Random.Shared.Next(100);
        var description = $"MCFH#{orderId}"; // PayOS giới hạn mô tả 25 ký tự

        var link2 = await _payOs.CreatePaymentLinkAsync(
            orderCode,
            (long)order.QuotedPrice,
            description,
            _payOs.BuildReturnUrl(orderId),
            _payOs.BuildCancelUrl(orderId));

        payment = new Payment
        {
            TransactionRef = $"PAYOS-{orderCode}",
            Amount = order.QuotedPrice,
            Status = "pending",
            Type = "scrape_order",
            CreatedBy = userId,
            CreatedAt = now,
            OrderCode = orderCode,
            PaymentLinkId = link2.PaymentLinkId,
            CheckoutUrl = link2.CheckoutUrl
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        order.PaymentId = payment.PaymentId;
        order.Status = "pending_payment";
        order.StatusMessage = "Chờ thanh toán qua PayOS — quét mã QR hoặc thanh toán trên trang checkout.";
        await _context.SaveChangesAsync();

        return await BuildCheckoutDtoAsync(order, payment, link2.CheckoutUrl, link2.QrCode);
    }

    /// <summary>Local bypass: tạo payment success + fulfill (scrape) — không gọi PayOS.</summary>
    private async Task<ScrapeOrderCheckoutDto?> PayOrderBypassAsync(int userId, ScrapeOrder order)
    {
        var now = DateTime.Now;
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100 + Random.Shared.Next(100);

        Payment? payment = null;
        if (order.PaymentId != null)
        {
            payment = await _context.Payments.FindAsync(order.PaymentId);
            if (payment != null && payment.Status == "success")
            {
                await FulfillPaidOrderAsync(order, payment);
                return await BuildCheckoutDtoAsync(order, payment, "", "");
            }
        }

        payment = new Payment
        {
            TransactionRef = $"BYPASS-{orderCode}",
            Amount = order.QuotedPrice,
            Status = "pending",
            Type = "scrape_order",
            CreatedBy = userId,
            CreatedAt = now,
            OrderCode = orderCode,
            PaymentLinkId = "local-bypass",
            CheckoutUrl = null
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        order.PaymentId = payment.PaymentId;
        order.Status = "pending_payment";
        order.StatusMessage = "Dev bypass PayOS — đang kích hoạt đơn...";
        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "PayOS Bypass bật: order {OrderId} được đánh dấu đã thanh toán (local only).",
            order.OrderId);

        await FulfillPaidOrderAsync(order, payment);
        return await BuildCheckoutDtoAsync(order, payment, "", "");
    }

    private async Task<ScrapeOrderCheckoutDto?> BuildCheckoutDtoAsync(
        ScrapeOrder order, Payment payment, string checkoutUrl, string qrCode)
    {
        var orderDto = await MapOrderAsync(order.OrderId, order.UserId);
        if (orderDto == null)
            return null;
        return new ScrapeOrderCheckoutDto
        {
            Order = orderDto,
            OrderCode = payment.OrderCode ?? 0,
            PaymentLinkId = payment.PaymentLinkId ?? "",
            CheckoutUrl = checkoutUrl,
            QrCode = qrCode,
            Amount = payment.Amount
        };
    }

    /// <summary>
    /// Xử lý webhook PayOS ĐÃ verify chữ ký: đối soát payment theo orderCode, kiểm tra số tiền,
    /// hoàn tất đơn idempotent (đã xử lý rồi thì no-op). Webhook là nguồn tin cậy về thanh toán.
    /// </summary>
    public async Task HandlePayOsWebhookAsync(WebhookData data)
    {
        if (data.Code != "00")
        {
            _logger.LogInformation("Webhook PayOS orderCode {OrderCode} không thành công (code {Code}) — bỏ qua.", data.OrderCode, data.Code);
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderCode == data.OrderCode && p.Type == "scrape_order");
        if (payment == null)
        {
            // Webhook test khi đăng ký URL (orderCode 123) hoặc payment của luồng khác — no-op.
            _logger.LogInformation("Webhook PayOS orderCode {OrderCode} không khớp payment nào — bỏ qua.", data.OrderCode);
            return;
        }

        if (data.Amount != (long)payment.Amount)
        {
            _logger.LogError(
                "Webhook PayOS orderCode {OrderCode}: số tiền không khớp (webhook {WebhookAmount} ≠ báo giá {QuotedAmount}) — KHÔNG kích hoạt đơn.",
                data.OrderCode, data.Amount, payment.Amount);
            return;
        }

        var order = await _context.ScrapeOrders.FirstOrDefaultAsync(o => o.PaymentId == payment.PaymentId);
        if (order == null)
        {
            _logger.LogWarning("Webhook PayOS orderCode {OrderCode}: không tìm thấy scrape order gắn với payment {PaymentId}.", data.OrderCode, payment.PaymentId);
            return;
        }

        if (payment.Amount != order.QuotedPrice)
        {
            _logger.LogError(
                "Webhook PayOS orderCode {OrderCode}: payment.Amount {PaymentAmount} ≠ order.QuotedPrice {QuotedPrice} — KHÔNG kích hoạt đơn.",
                data.OrderCode, payment.Amount, order.QuotedPrice);
            return;
        }

        await FulfillPaidOrderAsync(order, payment);
    }

    /// <summary>
    /// Confirm cho trang return: KHÔNG tin query param — tra cứu lại PayOS / DB.
    /// Nếu PayOS báo đã trả → hoàn tất đơn (idempotent với webhook). Nếu hủy/hết hạn → trả đơn về "quoted".
    /// </summary>
    public async Task<ScrapeOrderDto?> ConfirmPaymentAsync(int userId, int orderId)
    {
        var order = await _context.ScrapeOrders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        if (order == null)
            return null;

        if (order.Status == "pending_payment" && order.PaymentId != null)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == order.PaymentId);
            if (payment?.OrderCode != null && payment.Status == "pending")
            {
                var link = await _payOs.GetPaymentLinkAsync(payment.OrderCode.Value);
                if (link?.Status == PaymentLinkStatus.Paid)
                {
                    if (link.AmountPaid == (long)payment.Amount && payment.Amount == order.QuotedPrice)
                        await FulfillPaidOrderAsync(order, payment);
                    else
                        _logger.LogError(
                            "PayOS orderCode {OrderCode}: số tiền không khớp (AmountPaid {AmountPaid}, payment {PaymentAmount}, quoted {QuotedAmount}) — không kích hoạt đơn.",
                            payment.OrderCode, link.AmountPaid, payment.Amount, order.QuotedPrice);
                }
                else if (link?.Status is PaymentLinkStatus.Cancelled or PaymentLinkStatus.Expired or PaymentLinkStatus.Failed)
                {
                    payment.Status = "failed";
                    order.Status = "quoted";
                    order.StatusMessage = "Thanh toán đã bị hủy hoặc hết hạn — bạn có thể thanh toán lại.";
                    await _context.SaveChangesAsync();
                }
            }
        }
        else if (order.Status == "paid" && string.IsNullOrEmpty(order.ScrapeJobId))
        {
            // Đã thu tiền nhưng job chưa khởi động được (VD: backend bận) — thử lại khi user poll.
            await StartScrapeForPaidOrderAsync(order);
        }

        return await GetOrderAsync(userId, orderId);
    }

    /// <summary>Order đang được fulfill (webhook + confirm có thể chạy song song) — chống double-start job.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> FulfillRunning = new();

    /// <summary>
    /// Idempotent: đánh dấu payment success + order paid, rồi khởi động job cào → "scraping".
    /// Gọi lặp lại (webhook retry / confirm poll) sẽ no-op nếu đã xử lý.
    /// Sau khi thanh toán, nếu order có MentionsPackage thì tạo 1 row PROJECT_MENTION_PACKAGES (active)
    /// cho Project và cập nhật Project.MentionsQuotaTotal / MentionsFullUnlimited.
    /// </summary>
    private async Task FulfillPaidOrderAsync(ScrapeOrder order, Payment payment)
    {
        if (!FulfillRunning.TryAdd(order.OrderId, 0))
            return;

        try
        {
            if (payment.Status != "success")
            {
                var now = DateTime.Now;
                payment.Status = "success";
                payment.PaidAt = now;
                order.Status = "paid";
                order.PaidAt = now;
                order.StatusMessage = "Thanh toán thành công — đang khởi động cào dữ liệu...";
                await _context.SaveChangesAsync();
            }

            // Cấp package cho Project (idempotent — đã cấp rồi thì bỏ qua).
            if (!string.IsNullOrEmpty(order.MentionsPackage))
                await EnsureProjectPackageFromOrderAsync(order, payment);

            if (order.Status == "paid" && string.IsNullOrEmpty(order.ScrapeJobId))
                await StartScrapeForPaidOrderAsync(order);
        }
        finally
        {
            FulfillRunning.TryRemove(order.OrderId, out _);
        }
    }

    /// <summary>
    /// Cấp/cộng dồn quota cho Project. Tránh double-create bằng cách check existing Package theo PaymentId.
    /// </summary>
    private async Task EnsureProjectPackageFromOrderAsync(ScrapeOrder order, Payment payment)
    {
        var exists = await _context.ProjectMentionPackages
            .AnyAsync(p => p.PaymentId == payment.PaymentId);
        if (exists) return;

        var pkgType = order.MentionsPackage!;
        var isFull = pkgType == MentionPackageTypes.FullUnlimited;
        // MentionsIncluded đã được fill từ catalog khi tạo order (DB là source of truth).
        var included = isFull ? -1 : (order.MentionsIncluded ?? await ResolveMentionsIncludedAsync(pkgType));
        var now = DateTime.Now;

        var pkg = new ProjectMentionPackage
        {
            ProjectId = order.ProjectId,
            PaymentId = payment.PaymentId,
            PackageType = pkgType,
            MentionsIncluded = included,
            MentionsUsed = 0,
            Status = "active",
            CreatedAt = now
        };
        _context.ProjectMentionPackages.Add(pkg);

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == order.ProjectId);
        if (project != null)
        {
            if (isFull)
            {
                project.MentionsFullUnlimited = true;
                // Không cộng vào tổng quota (Full = vô hạn); vẫn có thể cộng để hiển thị "đã từng mua"
            }
            else
            {
                project.MentionsQuotaTotal += included;
            }
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>Fallback cho order cũ tạo trước khi catalog lookup — lấy max_items từ SCRAPE_PACKAGES.</summary>
    private async Task<int> ResolveMentionsIncludedAsync(string packageCode)
    {
        var pkg = await _catalog.GetActiveByCodeAsync(packageCode);
        return pkg?.MaxItems ?? 0;
    }

    private async Task StartScrapeForPaidOrderAsync(ScrapeOrder order)
    {
        // Contract đã confirm: runtime scrape nhận postedDays cố định 30 — chỉ quota mới ảnh hưởng Project.
        // member khác không đụng.
        var postedDays = 30;
        var jobId = await _jobRunner.StartAsync(order.ProjectId, order.UserId, postedDays);
        if (jobId == null)
        {
            order.StatusMessage = "Thanh toán thành công nhưng chưa khởi động được cào dữ liệu — hệ thống sẽ tự thử lại.";
            await _context.SaveChangesAsync();
            return;
        }

        // Estimate minutes lấy từ catalog (đồng bộ với giá client thấy khi quote).
        var pkg = order.MentionsPackage != null
            ? await _catalog.GetActiveByCodeAsync(order.MentionsPackage)
            : null;
        var estimatedMinutes = pkg != null
            ? EstimateMinutesByPackage(pkg)
            : 60;

        var now = DateTime.Now;
        order.ScrapeJobId = jobId;
        order.Status = "scraping";
        order.ProgressPercent = 5;
        order.EstimatedReportAt = now.AddMinutes(estimatedMinutes);
        order.StatusMessage = $"Thanh toán thành công. Báo cáo dự kiến sẵn sàng trước {order.EstimatedReportAt:HH:mm dd/MM/yyyy}.";
        await _context.SaveChangesAsync();
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

        // Load catalog 1 lần để tránh N+1 label lookup.
        var packageLabelMap = await BuildPackageLabelMapAsync(orders);

        return orders.Select(order => MapOrderFromEntity(order, projectNames, packageLabelMap, userId)).ToList();
    }

    /// <summary>Build dictionary Code → Name cho các package xuất hiện trong orders (cache hit).</summary>
    private async Task<IReadOnlyDictionary<string, string>> BuildPackageLabelMapAsync(IEnumerable<ScrapeOrder> orders)
    {
        var codes = orders
            .Where(o => !string.IsNullOrEmpty(o.MentionsPackage))
            .Select(o => o.MentionsPackage!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0) return new Dictionary<string, string>();

        var all = await _catalog.GetAllActiveAsync();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in all)
            if (codes.Contains(pkg.Code))
                map[pkg.Code] = pkg.Name;
        return map;
    }

    /// <summary>
    /// Lấy quota mentions hiện tại của Project + danh sách package active.
    /// Trả về null nếu user không phải member của workspace hoặc project không tồn tại.
    /// </summary>
    public async Task<ProjectMentionsQuotaDto?> GetProjectMentionsQuotaAsync(int userId, int projectId)
    {
        var project = await _context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.IsDeleted != true);
        if (project == null) return null;

        var member = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId);
        if (!member) return null;

        var packages = await _context.ProjectMentionPackages.AsNoTracking()
            .Where(p => p.ProjectId == projectId && p.Status == "active")
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Load catalog 1 lần để tra label — không gọi catalog trong .Select (tránh async-over-sync deadlock).
        var catalog = await _catalog.GetAllActiveAsync();
        var catalogMap = catalog.ToDictionary(p => p.Code, p => p.Name, StringComparer.OrdinalIgnoreCase);

        var dto = new ProjectMentionsQuotaDto
        {
            ProjectId = project.ProjectId,
            MentionsQuotaTotal = project.MentionsQuotaTotal,
            MentionsQuotaUsed = project.MentionsQuotaUsed,
            FullUnlimited = project.MentionsFullUnlimited,
            ExpiresAt = project.MentionsExpiresAt,
            MentionsRemaining = CalcRemainingMentions(project)
        };
        dto.ActivePackages = packages.Select(p => new MentionsPackageDto
        {
            PackageId = p.PackageId,
            PackageType = p.PackageType,
            PackageLabel = catalogMap.TryGetValue(p.PackageType, out var label)
                ? label
                : DefaultPackageLabel(p.PackageType),
            MentionsIncluded = p.MentionsIncluded,
            MentionsUsed = p.MentionsUsed,
            MentionsRemaining = p.MentionsIncluded == -1 ? null : (p.MentionsIncluded - p.MentionsUsed),
            ExpiresAt = p.ExpiresAt,
            CreatedAt = p.CreatedAt
        }).ToList();
        return dto;
    }

    /// <summary>
    /// Trả label hiển thị cho mã gói — ưu tiên tên từ SCRAPE_PACKAGES, fallback về label mặc định.
    /// </summary>
    private async Task<string> GetPackageLabelAsync(string? packageType)
    {
        if (string.IsNullOrEmpty(packageType)) return "";
        var pkg = await _catalog.GetActiveByCodeAsync(packageType);
        return pkg?.Name ?? DefaultPackageLabel(packageType);
    }

    private static string DefaultPackageLabel(string code) => code switch
    {
        MentionPackageTypes.Pack100 => "Gói 100 mentions",
        MentionPackageTypes.Pack300 => "Gói 300 mentions",
        MentionPackageTypes.Pack600 => "Gói 600 mentions",
        MentionPackageTypes.FullUnlimited => "Full Unlimited",
        _ => code
    };

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

            if (job.Status is "failed" or "cancelled")
            {
                order.Status = job.Status;
                order.StatusMessage = job.Status == "failed"
                    ? (string.IsNullOrWhiteSpace(job.ErrorMessage) ? "Lỗi trong quá trình cào dữ liệu." : $"Cào dữ liệu thất bại: {job.ErrorMessage}")
                    : "Đã hủy cào dữ liệu.";
                await _context.SaveChangesAsync();
            }
            else if (job.Status == "completed")
            {
                order.Status = "analyzing";
                order.ProgressPercent = 85;
                order.StatusMessage = "Cào xong — AI đang phân tích sentiment và báo cáo...";
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
        catch (Exception ex)
        {
            using var scope = _scopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScrapeOrderService>>();
            logger.LogError(ex, "Lỗi khi chạy Post Scrape cho order {OrderId}", orderId);
            
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

        var projectNames = new Dictionary<int, string> { [order.ProjectId] = projectName };
        return MapOrderFromEntity(order, projectNames, null, userId);
    }

    private ScrapeOrderDto MapOrderFromEntity(
        ScrapeOrder order,
        IReadOnlyDictionary<int, string> projectNames,
        IReadOnlyDictionary<string, string>? packageLabels = null,
        int? userId = null)
    {
        var projectName = projectNames.TryGetValue(order.ProjectId, out var name)
            ? name
            : $"Dự án #{order.ProjectId}";

        return MapOrderFromEntity(order, projectName, packageLabels, userId);
    }

    private ScrapeOrderDto MapOrderFromEntity(
        ScrapeOrder order,
        string projectName,
        IReadOnlyDictionary<string, string>? packageLabels = null,
        int? userId = null)
    {
        ScrapeJobStatusDto? jobDto = null;
        if (userId.HasValue && !string.IsNullOrEmpty(order.ScrapeJobId))
        {
            var job = _jobRunner.GetJob(order.ScrapeJobId, userId.Value);
            jobDto = job?.ToDto();
        }

        string? packageLabel = null;
        if (!string.IsNullOrEmpty(order.MentionsPackage))
        {
            if (packageLabels != null && packageLabels.TryGetValue(order.MentionsPackage!, out var lbl))
                packageLabel = lbl;
            else
                packageLabel = DefaultPackageLabel(order.MentionsPackage!);
        }

        return new ScrapeOrderDto
        {
            OrderId = order.OrderId,
            WorkspaceId = order.WorkspaceId,
            ProjectId = order.ProjectId,
            ProjectName = projectName,
            Keyword = order.Keyword,
            PostedSinceDays = order.PostedSinceDays,
            MentionsPackage = order.MentionsPackage,
            MentionsIncluded = order.MentionsIncluded,
            PackageLabel = packageLabel,
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

    // === Pricing/ETA theo SCRAPE_PACKAGES (đọc từ catalog) ===

    /// <summary>Số phút ước tính dựa trên <c>duration_days</c> của gói trong DB.</summary>
    public static int EstimateMinutesByPackage(ScrapePackage pkg)
    {
        // Quy tắc đơn giản: 1 ngày ≈ 10 phút xử lý; full unlimited gấp đôi.
        var baseMinutes = pkg.DurationDays * 10;
        var code = pkg.Code ?? "";
        return code == MentionPackageTypes.FullUnlimited ? baseMinutes * 2 : baseMinutes;
    }

    public static string FormatEtaLabelByPackage(ScrapePackage pkg)
    {
        var mins = EstimateMinutesByPackage(pkg);
        if (mins < 60)
            return $"Khoảng {mins} phút";
        var hours = mins / 60.0;
        return hours < 2 ? "Khoảng 1–2 giờ" : "Khoảng 2–4 giờ";
    }

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
