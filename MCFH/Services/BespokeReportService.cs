using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using MCFH.Configuration;
using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Services.Payments;
using MCFH.Services.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace MCFH.Services;

public class BespokeReportService
{
    private readonly McfhDbContext _context;
    private readonly ProjectAnalyticsService _analytics;
    private readonly IEmailService? _emailService;
    private readonly ScrapeJobRunner? _jobRunner;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ProjectReportService? _reportService;
    private readonly PayOsService? _payOs;
    private readonly PayOsOptions _payOsOptions;
    private readonly ILogger<BespokeReportService>? _logger;

    private const decimal BasicPackagePrice = 10_000m;
    private const decimal ProPackagePrice = 20_000m;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Chặn nhiều watcher chạy song song cho cùng 1 request (poll trùng / retry).</summary>
    private static readonly ConcurrentDictionary<int, byte> PostScrapeRunning = new();

    /// <summary>Chặn nhiều thanh toán fulfill song song cho cùng 1 request (webhook + confirm chạy đè nhau).</summary>
    private static readonly ConcurrentDictionary<int, byte> FulfillRunning = new();

    public BespokeReportService(
        McfhDbContext context,
        ProjectAnalyticsService analytics,
        IEmailService? emailService = null,
        ScrapeJobRunner? jobRunner = null,
        IServiceScopeFactory? scopeFactory = null,
        ProjectReportService? reportService = null,
        PayOsService? payOs = null,
        IOptions<PayOsOptions>? payOsOptions = null,
        ILogger<BespokeReportService>? logger = null)
    {
        _context = context;
        _analytics = analytics;
        _emailService = emailService;
        _jobRunner = jobRunner;
        _scopeFactory = scopeFactory;
        _reportService = reportService;
        _payOs = payOs;
        _payOsOptions = payOsOptions?.Value ?? new PayOsOptions();
        _logger = logger;
    }

    public async Task<BespokeCenterDto?> GetBespokeCenterAsync(int workspaceId, int projectId, int userId)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null) return null;

        var requests = await LoadProjectRequestsAsync(projectId);
        var dto = new BespokeCenterDto
        {
            UserSystemRole = user.SystemRole,
            Requests = requests.Select(r => MapRequest(r, user)).ToList()
        };

        if (IsAdmin(user))
        {
            dto.Reporters = await _context.Users
                .Where(u => u.SystemRole == "Reporter" && u.IsBanned != true)
                .OrderBy(u => u.FullName)
                .Select(u => new ReporterOptionDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();
        }

        return dto;
    }

    /// <summary>
    /// Tạo báo cáo chuyên sâu: luôn tạo Project mới trong workspace (không gắn project monitoring có sẵn).
    /// <paramref name="projectId"/> từ URL cũ bị bỏ qua — giữ signature để endpoint cũ không gãy.
    /// </summary>
    public async Task<BespokeRequestItemDto?> CreateRequestAsync(
        int workspaceId, int projectId, int userId, CreateBespokeRequestDto dto)
        => await CreateStandaloneRequestAsync(workspaceId, userId, dto);

    /// <summary>
    /// Tạo Project mới (keyword = SearchQuery) + BespokeRequest pending_payment.
    /// Scrape/PDF chỉ chạy sau thanh toán — không đổi pipeline.
    /// </summary>
    public async Task<BespokeRequestItemDto?> CreateStandaloneRequestAsync(
        int workspaceId, int userId, CreateBespokeRequestDto dto)
    {
        var user = await GetWorkspaceEditorAsync(workspaceId, userId);
        if (user == null) return null;

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Keyword))
            return null;

        var keyword = dto.Keyword.Trim();
        var title = dto.Title.Trim();
        var packageType = NormalizePackageType(dto.PackageType);
        var packagePrice = packageType == "pro" ? ProPackagePrice : BasicPackagePrice;

        var project = new Project
        {
            WorkspaceId = workspaceId,
            Name = title,
            Description = $"Báo cáo chuyên sâu — keyword: {keyword}",
            SearchQuery = keyword,
            EnableFacebook = true,
            EnableYoutube = true,
            EnableTiktok = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        _context.WorkspaceActivityLogs.Add(new WorkspaceActivityLog
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            ActionType = "CREATE_PROJECT",
            TargetType = "project",
            TargetId = project.ProjectId,
            TargetName = project.Name,
            Description = $"Tạo project bespoke \"{project.Name}\"",
            CreatedAt = DateTime.Now
        });

        var meta = new BespokeMeta
        {
            ProjectId = project.ProjectId,
            WorkspaceId = workspaceId,
            Keyword = keyword,
            PackageType = packageType,
            PackagePrice = packagePrice,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo,
            Modules = dto.Modules.Count > 0 ? dto.Modules : DefaultModules(),
            Format = string.IsNullOrWhiteSpace(dto.Format) ? "pdf" : dto.Format
        };

        var request = new BespokeRequest
        {
            ClientId = userId,
            Title = title,
            Requirements = dto.Requirements?.Trim(),
            CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions),
            AgreedPrice = packagePrice,
            Status = "pending_payment",
            Deadline = ParseDate(dto.DateTo)?.AddDays(7)
        };

        _context.BespokeRequests.Add(request);
        await _context.SaveChangesAsync();

        // KHÔNG khởi động scrape ở đây — chỉ bắt đầu sau khi thanh toán được xác nhận (PayRequestAsync/webhook).
        var reloaded = await GetProjectRequestAsync(project.ProjectId, request.RequestId);
        if (reloaded == null) return null;

        await LoadRequestNavigationsAsync(reloaded);
        return MapRequest(reloaded, user);
    }

    /// <summary>
    /// Tạo checkout PayOS cho yêu cầu bespoke: Payment status "pending".
    /// Frontend redirect sang CheckoutUrl; job cào CHỈ chạy sau khi webhook/confirm xác thực đã trả tiền.
    /// </summary>
    public async Task<BespokeCheckoutDto?> PayRequestAsync(int workspaceId, int projectId, int userId, int requestId)
    {
        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.ClientId != userId) return null;
        if (request.Status is not ("pending_payment")) return null;

        var meta = ParseMeta(request.CustomMetrics);
        if (meta.WorkspaceId != workspaceId) return null;

        var amount = request.AgreedPrice ?? meta.PackagePrice ?? BasicPackagePrice;

        // Đã có payment success trước đó (VD: thanh toán rồi nhưng khởi động cào thất bại) —
        // chỉ cần retry fulfill, KHÔNG tạo thêm payment mới / tính phí lại.
        var successPayment = await _context.Payments
            .Where(p => p.RequestId == requestId && p.Type == "bespoke" && p.Status == "success")
            .OrderByDescending(p => p.PaymentId)
            .FirstOrDefaultAsync();
        if (successPayment != null)
        {
            await FulfillPaidBespokeAsync(request, successPayment);
            return await BuildCheckoutDtoAsync(projectId, requestId, userId, successPayment, "", "");
        }

        if (_payOsOptions.Bypass || _payOs == null)
            return await PayRequestBypassAsync(userId, request, meta, amount);

        // Đã có checkout đang chờ → kiểm tra lại trên PayOS trước khi tạo link mới.
        var existingPayment = await _context.Payments
            .Where(p => p.RequestId == requestId && p.Type == "bespoke")
            .OrderByDescending(p => p.PaymentId)
            .FirstOrDefaultAsync(p => p.Status == "pending");
        if (existingPayment?.OrderCode != null)
        {
            var link = await _payOs.GetPaymentLinkAsync(existingPayment.OrderCode.Value);
            if (link?.Status == PaymentLinkStatus.Paid)
            {
                // Người dùng đã trả nhưng webhook chưa tới — hoàn tất luôn.
                await FulfillPaidBespokeAsync(request, existingPayment);
                return await BuildCheckoutDtoAsync(projectId, requestId, userId, existingPayment, existingPayment.CheckoutUrl ?? "", "");
            }
            if (link?.Status == PaymentLinkStatus.Pending && !string.IsNullOrEmpty(existingPayment.CheckoutUrl))
                return await BuildCheckoutDtoAsync(projectId, requestId, userId, existingPayment, existingPayment.CheckoutUrl, "");
            // Không tra cứu được PayOS (null) → giữ link cũ nếu còn checkoutUrl, tránh tạo link trùng.
            if (link == null && !string.IsNullOrEmpty(existingPayment.CheckoutUrl))
            {
                _logger?.LogWarning(
                    "Không tra cứu được PayOS orderCode {OrderCode} — tái sử dụng checkoutUrl hiện có (bespoke #{RequestId}).",
                    existingPayment.OrderCode, requestId);
                return await BuildCheckoutDtoAsync(projectId, requestId, userId, existingPayment, existingPayment.CheckoutUrl, "");
            }
            // Link cũ hết hạn / bị hủy → đánh dấu failed rồi tạo link mới bên dưới.
            if (link?.Status is PaymentLinkStatus.Cancelled or PaymentLinkStatus.Expired or PaymentLinkStatus.Failed)
            {
                existingPayment.Status = "failed";
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(existingPayment.CheckoutUrl))
            {
                return await BuildCheckoutDtoAsync(projectId, requestId, userId, existingPayment, existingPayment.CheckoutUrl, "");
            }
        }

        var now = DateTime.Now;
        // orderCode PayOS phải là số duy nhất — unix ms + 2 số ngẫu nhiên, vẫn dưới ngưỡng MAX_SAFE_INTEGER.
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100 + Random.Shared.Next(100);
        var description = $"BESPOKE#{requestId}"; // PayOS giới hạn mô tả 25 ký tự

        var link2 = await _payOs.CreatePaymentLinkAsync(
            orderCode,
            (long)amount,
            description,
            _payOs.BuildBespokeReturnUrl(requestId, workspaceId, projectId),
            _payOs.BuildBespokeCancelUrl(requestId, workspaceId, projectId));

        var payment = new Payment
        {
            TransactionRef = $"PAYOS-{orderCode}",
            Amount = amount,
            Status = "pending",
            Type = "bespoke",
            RequestId = requestId,
            CreatedBy = userId,
            CreatedAt = now,
            OrderCode = orderCode,
            PaymentLinkId = link2.PaymentLinkId,
            CheckoutUrl = link2.CheckoutUrl
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return await BuildCheckoutDtoAsync(projectId, requestId, userId, payment, link2.CheckoutUrl, link2.QrCode);
    }

    /// <summary>Local bypass: tạo payment success + fulfill (bắt đầu cào) — không gọi PayOS.</summary>
    private async Task<BespokeCheckoutDto?> PayRequestBypassAsync(
        int userId, BespokeRequest request, BespokeMeta meta, decimal amount)
    {
        var now = DateTime.Now;
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 100 + Random.Shared.Next(100);

        var payment = new Payment
        {
            TransactionRef = $"BYPASS-{orderCode}",
            Amount = amount,
            Status = "pending",
            Type = "bespoke",
            RequestId = request.RequestId,
            CreatedBy = userId,
            CreatedAt = now,
            OrderCode = orderCode,
            PaymentLinkId = "local-bypass",
            CheckoutUrl = null
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger?.LogWarning(
            "PayOS Bypass bật: bespoke request {RequestId} được đánh dấu đã thanh toán (local only).",
            request.RequestId);

        await FulfillPaidBespokeAsync(request, payment);
        return await BuildCheckoutDtoAsync(meta.ProjectId, request.RequestId, userId, payment, "", "");
    }

    private async Task<BespokeCheckoutDto?> BuildCheckoutDtoAsync(
        int projectId, int requestId, int userId, Payment payment, string checkoutUrl, string qrCode)
    {
        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null) return null;
        await LoadRequestNavigationsAsync(request);

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new BespokeCheckoutDto
        {
            Request = MapRequest(request, user),
            OrderCode = payment.OrderCode ?? 0,
            PaymentLinkId = payment.PaymentLinkId ?? "",
            CheckoutUrl = checkoutUrl,
            QrCode = qrCode,
            Amount = payment.Amount
        };
    }

    /// <summary>
    /// Xử lý webhook PayOS ĐÃ verify chữ ký: đối soát payment theo orderCode, kiểm tra số tiền,
    /// hoàn tất yêu cầu idempotent (đã xử lý rồi thì no-op). No-op nếu payment không phải type "bespoke".
    /// </summary>
    public async Task HandlePayOsWebhookAsync(WebhookData data)
    {
        if (data.Code != "00")
        {
            _logger?.LogInformation("Webhook PayOS orderCode {OrderCode} không thành công (code {Code}) — bỏ qua (bespoke).", data.OrderCode, data.Code);
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderCode == data.OrderCode && p.Type == "bespoke");
        if (payment == null)
        {
            // Webhook test khi đăng ký URL (orderCode 123) hoặc payment của luồng khác (scrape_order) — no-op.
            return;
        }

        if (data.Amount != (long)payment.Amount)
        {
            _logger?.LogError(
                "Webhook PayOS orderCode {OrderCode}: số tiền không khớp (webhook {WebhookAmount} ≠ payment {PaymentAmount}) — KHÔNG kích hoạt bespoke.",
                data.OrderCode, data.Amount, payment.Amount);
            return;
        }

        if (payment.RequestId == null)
        {
            _logger?.LogWarning("Webhook PayOS orderCode {OrderCode}: payment {PaymentId} không có RequestId gắn kèm.", data.OrderCode, payment.PaymentId);
            return;
        }

        var request = await _context.BespokeRequests.FirstOrDefaultAsync(r => r.RequestId == payment.RequestId);
        if (request == null)
        {
            _logger?.LogWarning("Webhook PayOS orderCode {OrderCode}: không tìm thấy bespoke request {RequestId}.", data.OrderCode, payment.RequestId);
            return;
        }

        if (payment.Amount != (request.AgreedPrice ?? 0))
        {
            _logger?.LogError(
                "Webhook PayOS orderCode {OrderCode}: payment.Amount {PaymentAmount} ≠ request.AgreedPrice {AgreedPrice} — KHÔNG kích hoạt bespoke.",
                data.OrderCode, payment.Amount, request.AgreedPrice);
            return;
        }

        await FulfillPaidBespokeAsync(request, payment);
    }

    /// <summary>
    /// Confirm cho trang return: KHÔNG tin query param — tra cứu lại PayOS / DB.
    /// Nếu PayOS báo đã trả → hoàn tất yêu cầu (idempotent với webhook). Nếu hủy/hết hạn → giữ pending_payment để thanh toán lại.
    /// </summary>
    public async Task<BespokeRequestItemDto?> ConfirmPaymentAsync(int workspaceId, int projectId, int userId, int requestId)
    {
        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.ClientId != userId) return null;

        var meta = ParseMeta(request.CustomMetrics);
        if (meta.WorkspaceId != workspaceId) return null;

        if (request.Status == "pending_payment")
        {
            // Đã thu tiền trước đó nhưng khởi động cào thất bại (VD: backend bận) — thử lại khi user poll.
            var successPayment = await _context.Payments
                .Where(p => p.RequestId == requestId && p.Type == "bespoke" && p.Status == "success")
                .OrderByDescending(p => p.PaymentId)
                .FirstOrDefaultAsync();
            if (successPayment != null)
            {
                await FulfillPaidBespokeAsync(request, successPayment);
            }
            else
            {
                var payment = await _context.Payments
                    .Where(p => p.RequestId == requestId && p.Type == "bespoke")
                    .OrderByDescending(p => p.PaymentId)
                    .FirstOrDefaultAsync(p => p.Status == "pending");
                if (payment?.OrderCode != null && _payOs != null)
                {
                    var link = await _payOs.GetPaymentLinkAsync(payment.OrderCode.Value);
                    if (link?.Status == PaymentLinkStatus.Paid)
                    {
                        if (link.AmountPaid == (long)payment.Amount && payment.Amount == (request.AgreedPrice ?? 0))
                            await FulfillPaidBespokeAsync(request, payment);
                        else
                            _logger?.LogError(
                                "PayOS orderCode {OrderCode}: số tiền không khớp (AmountPaid {AmountPaid}, payment {PaymentAmount}, agreed {AgreedPrice}) — không kích hoạt bespoke.",
                                payment.OrderCode, link.AmountPaid, payment.Amount, request.AgreedPrice);
                    }
                    else if (link?.Status is PaymentLinkStatus.Cancelled or PaymentLinkStatus.Expired or PaymentLinkStatus.Failed)
                    {
                        payment.Status = "failed";
                        await _context.SaveChangesAsync();
                        // Giữ nguyên "pending_payment" để khách bấm thanh toán lại.
                    }
                }
            }
        }

        var reloaded = await GetProjectRequestAsync(projectId, requestId);
        if (reloaded == null) return null;
        await LoadRequestNavigationsAsync(reloaded);

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;
        return MapRequest(reloaded, user);
    }

    /// <summary>Trạng thái từ "gathering_data" trở đi — nghĩa là cào đã được khởi động cho yêu cầu này.</summary>
    private static readonly HashSet<string> ScrapeStartedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "gathering_data", "report_ready", "awaiting_reporter", "assigned",
        "in_progress", "completed", "revision_requested"
    };

    /// <summary>
    /// Idempotent: nếu đã success + đã khởi động cào rồi → no-op. Ngược lại đánh dấu payment success
    /// rồi khởi động job cào. Gọi lặp lại an toàn (webhook retry / confirm poll / pay retry).
    /// </summary>
    private async Task FulfillPaidBespokeAsync(BespokeRequest request, Payment payment)
    {
        if (!FulfillRunning.TryAdd(request.RequestId, 0))
            return;

        try
        {
            var reloaded = await _context.BespokeRequests.FirstOrDefaultAsync(r => r.RequestId == request.RequestId);
            if (reloaded == null) return;

            var alreadyStarted = reloaded.Status != null && ScrapeStartedStatuses.Contains(reloaded.Status);
            if (payment.Status == "success" && alreadyStarted)
                return; // đã xử lý xong (webhook + confirm chạy trùng) — no-op

            if (payment.Status != "success")
            {
                payment.Status = "success";
                payment.PaidAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            if (!alreadyStarted && _jobRunner != null && _scopeFactory != null)
            {
                var meta = ParseMeta(reloaded.CustomMetrics);
                await StartScrapeForRequestAsync(reloaded.RequestId, meta, reloaded.ClientId);
            }
        }
        finally
        {
            FulfillRunning.TryRemove(request.RequestId, out _);
        }
    }

    /// <summary>
    /// Khởi động job cào theo keyword của đơn bespoke. An toàn cho đồng đội:
    /// KHÔNG xoá mềm ScrapedFeedbacks hiện có (chỉ đổi SearchQuery tạm thời rồi khôi phục sau).
    /// </summary>
    private async Task StartScrapeForRequestAsync(int requestId, BespokeMeta meta, int userId)
    {
        if (_jobRunner == null || _scopeFactory == null) return;

        var project = await _context.Projects.FindAsync(meta.ProjectId);
        if (project == null) return;

        // Đánh dấu mốc thời gian bắt đầu cào (cùng đồng hồ với ScrapedAt của feedbacks) —
        // dùng để lọc report chỉ lấy dữ liệu của lượt cào NÀY, tránh trộn mentions cũ (VD "iphone 13" khi khách tìm "iphone 8").
        meta.ScrapeStartedAt = DateTime.Now;

        meta.PreviousSearchQuery = project.SearchQuery;
        if (!string.IsNullOrWhiteSpace(meta.Keyword))
            project.SearchQuery = meta.Keyword.Trim();

        var request = await _context.BespokeRequests.FindAsync(requestId);
        if (request == null) return;
        request.CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions);

        // Lưu SearchQuery tạm thời + PreviousSearchQuery TRƯỚC khi gọi StartAsync,
        // để không mất dấu vết nếu backend crash giữa chừng.
        await _context.SaveChangesAsync();

        var jobId = await _jobRunner.StartAsync(meta.ProjectId, userId, ComputePostedSinceDays(meta));
        if (jobId == null)
        {
            await RestoreProjectSearchQueryAsync(meta);
            return;
        }

        meta.ScrapeJobId = jobId;
        request.CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions);
        request.Status = "gathering_data";
        await _context.SaveChangesAsync();

        _ = WatchScrapeThenExportAsync(requestId, jobId, userId);
    }

    /// <summary>Khôi phục SearchQuery cũ của project — CHỈ khi chưa bị đồng đội đổi tay trong lúc chờ cào.</summary>
    private async Task RestoreProjectSearchQueryAsync(BespokeMeta meta)
    {
        var project = await _context.Projects.FindAsync(meta.ProjectId);
        if (project == null) return;

        var bespokeKeyword = meta.Keyword?.Trim();
        if (!string.IsNullOrEmpty(bespokeKeyword) &&
            string.Equals(project.SearchQuery, bespokeKeyword, StringComparison.Ordinal))
        {
            project.SearchQuery = meta.PreviousSearchQuery;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreSearchQueryAfterBespokeAsync(int requestId)
    {
        var request = await _context.BespokeRequests.FindAsync(requestId);
        if (request == null) return;

        var meta = ParseMeta(request.CustomMetrics);
        if (meta.ProjectId <= 0) return;

        await RestoreProjectSearchQueryAsync(meta);
    }

    /// <summary>Theo dõi job cào nền, khi xong (hoặc timeout) sẽ tự xuất báo cáo hệ thống.</summary>
    private async Task WatchScrapeThenExportAsync(int requestId, string jobId, int userId)
    {
        if (_scopeFactory == null || _jobRunner == null) return;
        if (!PostScrapeRunning.TryAdd(requestId, 0)) return;

        try
        {
            const int maxIterations = 180; // ~30 phút (poll mỗi 10s)
            for (var i = 0; i < maxIterations; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                var job = _jobRunner.GetJob(jobId, userId);
                // job == null: store mất sau restart → coi như xong, finalize với data hiện có.
                if (job == null || job.Status is "completed" or "failed" or "cancelled")
                {
                    await FinalizeBespokeReportAsync(_scopeFactory, requestId);
                    return;
                }
            }

            // Hết thời gian chờ — vẫn cố xuất báo cáo với dữ liệu đã cào được đến hiện tại.
            await FinalizeBespokeReportAsync(_scopeFactory, requestId);
        }
        catch
        {
            // Không để lỗi tác vụ nền làm crash tiến trình — trạng thái vẫn được dọn ở finally.
        }
        finally
        {
            PostScrapeRunning.TryRemove(requestId, out _);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<BespokeReportService>();
                await svc.RestoreSearchQueryAfterBespokeAsync(requestId);
            }
            catch
            {
                // best-effort — không chặn luồng nếu khôi phục SearchQuery lỗi
            }
        }
    }

    /// <summary>
    /// Nhặt đơn bespoke kẹt <c>gathering_data</c> (thường sau restart backend — watcher in-memory mất).
    /// Job scrape xong / mất / chạy quá 25 phút → finalize PDF + report_ready.
    /// </summary>
    public async Task RecoverStuckBespokeRequestsAsync()
    {
        if (_scopeFactory == null) return;

        var stuck = await _context.BespokeRequests
            .Where(r => r.Status == "gathering_data")
            .ToListAsync();

        foreach (var request in stuck)
        {
            var meta = ParseMeta(request.CustomMetrics);
            if (meta.ProjectId <= 0 || meta.WorkspaceId <= 0) continue;

            ScrapingJob? job = null;
            if (!string.IsNullOrWhiteSpace(meta.ScrapeJobId))
            {
                job = await _context.ScrapingJobs
                    .FirstOrDefaultAsync(j => j.JobId == meta.ScrapeJobId);
            }

            var finishedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "completed", "failed", "cancelled", "completed_with_errors"
            };

            var jobDone = job == null
                || (!string.IsNullOrWhiteSpace(job.Status) && finishedStatuses.Contains(job.Status))
                || (job.StartedAt.HasValue && DateTime.Now - job.StartedAt.Value >= TimeSpan.FromMinutes(25));

            // In-memory job còn "running" trên process hiện tại → để watcher xử lý, trừ khi quá hạn DB.
            if (!jobDone) continue;
            if (!PostScrapeRunning.TryAdd(request.RequestId, 0)) continue;

            try
            {
                if (job != null && string.Equals(job.Status, "running", StringComparison.OrdinalIgnoreCase))
                {
                    var count = await _context.ScrapedFeedbacks
                        .CountAsync(f => f.ProjectId == meta.ProjectId && f.IsDeleted != true);
                    job.Status = count > 0 ? "completed" : "completed_with_errors";
                    job.TotalScraped = count;
                    job.FinishedAt = DateTime.Now;
                    job.ErrorLog = string.IsNullOrWhiteSpace(job.ErrorLog)
                        ? "[Recover] Job treo / mất watcher sau restart — force finalize."
                        : job.ErrorLog + "\n[Recover] Job treo / mất watcher sau restart — force finalize.";
                    await _context.SaveChangesAsync();
                }

                _logger?.LogWarning(
                    "Recover bespoke request {RequestId}: finalize sau khi kẹt gathering_data.",
                    request.RequestId);
                await FinalizeBespokeReportAsync(_scopeFactory, request.RequestId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Recover bespoke request {RequestId} thất bại.", request.RequestId);
            }
            finally
            {
                PostScrapeRunning.TryRemove(request.RequestId, out _);
            }
        }
    }

    /// <summary>Chạy trong scope riêng (tác vụ nền) — hoàn tất phân tích AI + xuất báo cáo hệ thống.</summary>
    private static async Task FinalizeBespokeReportAsync(IServiceScopeFactory scopeFactory, int requestId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();

        var request = await db.BespokeRequests
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null) return;

        var meta = ParseMeta(request.CustomMetrics);
        if (meta.ProjectId <= 0 || meta.WorkspaceId <= 0) return;

        try
        {
            var hasPending = await db.ScrapedFeedbacks
                .AnyAsync(f => f.ProjectId == meta.ProjectId && f.IsDeleted != true && f.AiAnalysis == null);
            if (hasPending)
            {
                var analyze = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();
                await analyze.AnalyzePendingFeedbacksAsync(meta.ProjectId, false);
            }
        }
        catch
        {
            // AI lỗi vẫn tiếp tục xuất báo cáo với sentiment hiện có.
        }

        try
        {
            var bespoke = scope.ServiceProvider.GetRequiredService<BespokeReportService>();
            await bespoke.EnsureSystemDraftPublicAsync(meta.WorkspaceId, meta.ProjectId, request.ClientId, requestId);
            await bespoke.RestoreSearchQueryAfterBespokeAsync(requestId);
        }
        catch
        {
            // Không chặn việc chuyển trạng thái report_ready nếu build PDF lỗi.
        }

        request.Status = "report_ready";
        request.ReporterId = null;
        await db.SaveChangesAsync();

        try
        {
            var notify = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notify.NotifyAsync(
                request.ClientId,
                "Báo cáo chuyên sâu đã sẵn sàng",
                $"Báo cáo «{request.Title}» đã được tổng hợp xong từ dữ liệu vừa cào. Vào trang Bespoke để xem.",
                "bespoke_ready",
                "bespoke_request",
                requestId,
                meta.ProjectId);
        }
        catch
        {
            // không chặn luồng nếu notify lỗi
        }
    }

    /// <summary>Khách gửi báo cáo hệ thống (report_ready) cho Reporter chỉnh tay, kèm ghi chú cần sửa.</summary>
    public async Task<BespokeRequestItemDto?> SendToReporterAsync(
        int workspaceId, int projectId, int userId, int requestId, SendBespokeToReporterDto dto)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null || IsAdmin(user) || IsReporter(user)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.ClientId != userId) return null;
        if (!string.Equals(request.Status, "report_ready", StringComparison.OrdinalIgnoreCase)) return null;
        if (request.BespokeReports.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(dto.Note)) return null;

        var meta = ParseMeta(request.CustomMetrics);
        meta.RevisionFeedback = dto.Note.Trim();
        request.CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions);
        request.Status = "awaiting_reporter";
        request.ReporterId = null;
        await _context.SaveChangesAsync();

        await NotifyReportersAwaitingAsync(request, meta.ProjectId, dto.Note.Trim());

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> AssignReporterAsync(
        int workspaceId, int projectId, int adminUserId, int requestId, int reporterId)
    {
        var admin = await GetUserWithAccessAsync(workspaceId, projectId, adminUserId);
        if (admin == null || !IsAdmin(admin)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || !string.Equals(request.Status, "awaiting_reporter", StringComparison.OrdinalIgnoreCase))
            return null;

        var reporter = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == reporterId && u.SystemRole == "Reporter");
        if (reporter == null) return null;

        request.ReporterId = reporterId;
        request.AssignedBy = adminUserId;
        request.AssignedAt = DateTime.Now;
        request.Status = "assigned";
        await _context.SaveChangesAsync();

        // Đảm bảo Reporter có sẵn bản nháp hệ thống mới nhất để tải về chỉnh.
        await EnsureSystemDraftAsync(workspaceId, projectId, adminUserId, requestId);

        await NotifyReporterAssignedAsync(request, projectId, reporterId);

        await _context.Entry(request).Reference(r => r.Reporter).LoadAsync();
        await _context.Entry(request).Reference(r => r.Client).LoadAsync();
        await _context.Entry(request).Collection(r => r.BespokeReports).LoadAsync();
        return MapRequest(request, admin);
    }

    public async Task<BespokeRequestItemDto?> StartWorkAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || !CanWorkOnRequest(user, request)) return null;
        if (request.Status?.ToLowerInvariant() != "assigned") return null;

        request.Status = "in_progress";
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> DeliverReportAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || !CanWorkOnRequest(user, request)) return null;
        var status = request.Status?.ToLowerInvariant();
        if (status is not ("assigned" or "in_progress")) return null;
        if (_reportService == null) return null;

        var version = $"v{(request.BespokeReports.Count + 1):D2}";
        var ok = await SaveAnalyticsPdfDraftAsync(workspaceId, projectId, userId, requestId, request, version, "bespoke");
        if (!ok) return null;

        request.Status = "completed";
        request.SubmittedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<(byte[] Content, string FileName)?> DownloadDeliverableAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null) return null;

        if (!IsAdmin(user) && !(IsReporter(user) && request.ReporterId == userId))
        {
            var member = await GetUserWithAccessAsync(workspaceId, projectId, userId);
            if (member == null) return null;
        }

        // Ưu tiên bản có file trên disk (Reporter upload hoặc draft đã render).
        // Không rebuild PDF mỗi lần tải — Playwright rất chậm; PDF tạo lúc finalize.
        var report = request.BespokeReports
            .OrderByDescending(r => r.UploadedAt)
            .FirstOrDefault(r =>
                !string.IsNullOrWhiteSpace(r.FileUrl) &&
                File.Exists(ResolveFilePath(r.FileUrl)));

        if (report == null && _reportService != null)
        {
            await EnsureSystemDraftAsync(workspaceId, projectId, userId, requestId);
            request = await GetProjectRequestAsync(projectId, requestId);
            report = request?.BespokeReports
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.FileUrl) &&
                    File.Exists(ResolveFilePath(r.FileUrl)));
        }

        if (report == null) return null;

        var path = ResolveFilePath(report.FileUrl);
        var bytes = await File.ReadAllBytesAsync(path);
        var fileName = Path.GetFileName(path);
        return (bytes, fileName);
    }

    public async Task<BespokeRequestItemDto?> RequestRevisionAsync(
        int workspaceId, int projectId, int userId, int requestId, RequestBespokeRevisionDto dto)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null || IsAdmin(user) || IsReporter(user)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.Status != "completed") return null;
        if (string.IsNullOrWhiteSpace(dto.Feedback)) return null;

        var meta = ParseMeta(request.CustomMetrics);
        meta.RevisionFeedback = dto.Feedback.Trim();
        request.CustomMetrics = JsonSerializer.Serialize(meta, JsonOptions);
        request.Status = "revision_requested";
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> UploadRevisionAsync(
        int workspaceId, int projectId, int userId, int requestId, Stream fileStream, string fileName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.Status is not ("revision_requested" or "assigned" or "in_progress" or "awaiting_reporter"))
            return null;
        if (!CanWorkOnRequest(user, request))
        {
            // Cho phép Reporter nhận đơn awaiting_reporter chưa giao rồi upload luôn.
            if (!(IsReporter(user)
                && request.ReporterId == null
                && string.Equals(request.Status, "awaiting_reporter", StringComparison.OrdinalIgnoreCase)))
                return null;

            request.ReporterId = userId;
            request.AssignedAt = DateTime.Now;
            request.Status = "in_progress";
        }

        var safeName = SanitizeFileName(fileName);
        if (!IsAllowedDeliverableExtension(safeName)) return null;

        var folder = GetBespokeFolder(requestId);
        Directory.CreateDirectory(folder);
        var storedName = $"revision-{DateTime.Now:yyyyMMddHHmmss}-{safeName}";
        var filePath = Path.Combine(folder, storedName);
        await using (var fs = File.Create(filePath))
            await fileStream.CopyToAsync(fs);

        var relativePath = Path.Combine("StorageData", "bespoke", requestId.ToString(), storedName);
        var version = $"v{(request.BespokeReports.Count + 1):D2}";

        _context.BespokeReports.Add(new BespokeReport
        {
            RequestId = requestId,
            FileUrl = relativePath,
            Version = version,
            UploadedAt = DateTime.Now
        });

        request.Status = "completed";
        request.SubmittedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        await NotifyClientReportReadyAsync(request, projectId);
        return MapRequest(request, user);
    }

    /// <summary>
    /// Đảm bảo có bản nháp hệ thống. Mặc định giữ PDF đã có (tải nhanh).
    /// forceRebuild=true khi vừa cào xong / cần áp dụng filter mới.
    /// </summary>
    private async Task EnsureSystemDraftAsync(
        int workspaceId, int projectId, int userId, int requestId, bool forceRebuild = false)
    {
        if (_reportService == null) return;

        var request = await _context.BespokeRequests
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null) return;

        var totalBefore = request.BespokeReports.Count;
        var draftReports = request.BespokeReports
            .Where(r => r.Version != null && r.Version.Contains("draft", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Reporter đã upload bản riêng — không ghi đè bằng bản nháp hệ thống.
        var nonDraftCount = totalBefore - draftReports.Count;
        if (nonDraftCount > 0) return;

        // Đã có PDF nháp hợp lệ → phục vụ luôn, không Playwright lại.
        if (!forceRebuild)
        {
            var readyDraft = draftReports
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.FileUrl) &&
                    File.Exists(ResolveFilePath(r.FileUrl)));
            if (readyDraft != null) return;
        }

        foreach (var draft in draftReports)
        {
            DeleteDeliverableFile(draft.FileUrl);
            _context.BespokeReports.Remove(draft);
        }
        if (draftReports.Count > 0)
            await _context.SaveChangesAsync();

        await SaveAnalyticsPdfDraftAsync(workspaceId, projectId, userId, requestId, request, "v01-draft", "system-draft");
    }

    public Task EnsureSystemDraftPublicAsync(int workspaceId, int projectId, int userId, int requestId) =>
        EnsureSystemDraftAsync(workspaceId, projectId, userId, requestId, forceRebuild: true);

    /// <summary>
    /// Render PDF analytics (không đụng Reports index của project) rồi lưu làm 1 bản BESPOKE_REPORTS.
    /// </summary>
    private async Task<bool> SaveAnalyticsPdfDraftAsync(
        int workspaceId, int projectId, int userId, int requestId,
        BespokeRequest request, string version, string fileNamePrefix)
    {
        if (_reportService == null) return false;

        var meta = ParseMeta(request.CustomMetrics);
        var displayName = $"{request.Title} ({meta.Keyword})";

        // Lọc report theo từ khoá + mốc bắt đầu cào của đơn bespoke NÀY — tránh trộn mentions cũ
        // của các lượt cào/scrape trước đó vào cùng project (đồng đội không cho soft-delete feedback cũ).
        var filter = new MentionQueryDto
        {
            Search = meta.Keyword,
            DateFrom = meta.ScrapeStartedAt,
            ExcludeMuted = true
        };
        var rendered = await _reportService.RenderAnalyticsPdfAsync(workspaceId, projectId, userId, displayName, filter);
        if (rendered == null) return false;

        var folder = GetBespokeFolder(requestId);
        Directory.CreateDirectory(folder);
        var fileName = $"{fileNamePrefix}-{requestId}.pdf";
        var filePath = Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(filePath, rendered.Value.Content);

        var relativePath = Path.Combine("StorageData", "bespoke", requestId.ToString(), fileName);

        _context.BespokeReports.Add(new BespokeReport
        {
            RequestId = requestId,
            FileUrl = relativePath,
            Version = version,
            UploadedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task NotifyClientReportReadyAsync(BespokeRequest request, int projectId)
    {
        const string title = "Báo cáo chuyên sâu đã sẵn sàng";
        var body = $"Reporter đã gửi báo cáo «{request.Title}». Vào trang Bespoke để tải về.";
        try
        {
            var notify = new NotificationService(_context);
            await notify.NotifyAsync(
                request.ClientId,
                title,
                body,
                "bespoke_delivered",
                "bespoke_request",
                request.RequestId,
                projectId);
        }
        catch
        {
            // không chặn luồng upload nếu notify lỗi
        }

        // Đăng ký bằng email (local) → gửi thêm mail
        try
        {
            var client = request.Client ?? await _context.Users.FindAsync(request.ClientId);
            if (_emailService != null
                && client != null
                && !string.IsNullOrWhiteSpace(client.Email)
                && string.Equals(client.AuthProvider, "local", StringComparison.OrdinalIgnoreCase))
            {
                await _emailService.SendEmailAsync(
                    client.Email,
                    title,
                    $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>");
            }
        }
        catch
        {
            // không chặn luồng upload nếu email lỗi
        }
    }

    /// <summary>Khách gửi yêu cầu chỉnh sửa → báo tất cả Reporter.</summary>
    private async Task NotifyReportersAwaitingAsync(BespokeRequest request, int projectId, string note)
    {
        try
        {
            var recipientIds = await _context.Users
                .Where(u => u.SystemRole == "Reporter")
                .Select(u => u.UserId)
                .ToListAsync();

            if (recipientIds.Count == 0) return;

            var notePreview = note.Length > 120 ? note[..120] + "…" : note;
            var title = "Khách gửi báo cáo cần chỉnh sửa";
            var body = $"«{request.Title}»: {notePreview}";
            var notify = new NotificationService(_context);

            foreach (var userId in recipientIds)
            {
                await notify.NotifyAsync(
                    userId,
                    title,
                    body,
                    "bespoke_revision_request",
                    "bespoke_request",
                    request.RequestId,
                    projectId);
            }
        }
        catch
        {
            // không chặn luồng gửi Reporter nếu notify lỗi
        }
    }

    /// <summary>Admin giao đơn → báo Reporter được chọn.</summary>
    private async Task NotifyReporterAssignedAsync(BespokeRequest request, int projectId, int reporterId)
    {
        try
        {
            var notify = new NotificationService(_context);
            await notify.NotifyAsync(
                reporterId,
                "Bạn được giao báo cáo chuyên sâu",
                $"Đơn «{request.Title}» đã được giao cho bạn. Vào Tasks để tải và chỉnh sửa.",
                "bespoke_assigned",
                "bespoke_request",
                request.RequestId,
                projectId);
        }
        catch
        {
            // không chặn luồng giao Reporter nếu notify lỗi
        }
    }

    private async Task<List<BespokeRequest>> LoadProjectRequestsAsync(int projectId)
    {
        var all = await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .OrderByDescending(r => r.RequestId)
            .ToListAsync();

        return all.Where(r => ParseMeta(r.CustomMetrics).ProjectId == projectId).ToList();
    }

    private async Task<BespokeRequest?> GetProjectRequestAsync(int projectId, int requestId)
    {
        var request = await _context.BespokeRequests
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null) return null;
        return ParseMeta(request.CustomMetrics).ProjectId == projectId ? request : null;
    }

    private async Task LoadRequestNavigationsAsync(BespokeRequest request)
    {
        await _context.Entry(request).Reference(r => r.Client).LoadAsync();
        await _context.Entry(request).Reference(r => r.Reporter).LoadAsync();
        await _context.Entry(request).Collection(r => r.BespokeReports).LoadAsync();
    }

    private BespokeRequestItemDto MapRequest(BespokeRequest r, User currentUser)
    {
        var meta = ParseMeta(r.CustomMetrics);
        var latestReport = r.BespokeReports.OrderByDescending(b => b.UploadedAt).FirstOrDefault();

        return new BespokeRequestItemDto
        {
            RequestId = r.RequestId,
            ProjectId = meta.ProjectId,
            Title = r.Title,
            Requirements = r.Requirements,
            Status = r.Status ?? "pending",
            StatusLabel = StatusLabel(r.Status),
            Deadline = r.Deadline,
            SubmittedAt = r.SubmittedAt,
            AssignedAt = r.AssignedAt,
            ClientName = r.Client?.FullName,
            ReporterName = r.Reporter?.FullName,
            ReporterId = r.ReporterId,
            Modules = meta.Modules,
            DateFrom = meta.DateFrom,
            DateTo = meta.DateTo,
            Format = meta.Format,
            Keyword = meta.Keyword,
            PackageType = meta.PackageType,
            PackagePrice = meta.PackagePrice,
            AgreedPrice = r.AgreedPrice,
            HasDeliverable = latestReport != null,
            DeliverableReportId = latestReport?.ReportId
        };
    }

    private static bool CanWorkOnRequest(User user, BespokeRequest request) =>
        IsAdmin(user) || (IsReporter(user) && request.ReporterId == user.UserId);

    private static bool IsAdmin(User u) =>
        u.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsReporter(User u) =>
        u.SystemRole.Equals("Reporter", StringComparison.OrdinalIgnoreCase);

    private async Task<User?> GetUserWithAccessAsync(int workspaceId, int projectId, int userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return null;

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId && p.WorkspaceId == workspaceId && p.IsDeleted != true);
        if (!projectExists) return null;

        return await _context.Users.FindAsync(userId);
    }

    /// <summary>Owner/Editor của workspace — dùng khi tạo bespoke standalone (chưa có project).</summary>
    private async Task<User?> GetWorkspaceEditorAsync(int workspaceId, int userId)
    {
        var canEdit = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId &&
                           m.UserId == userId &&
                           (m.Role.RoleName == "Owner" || m.Role.RoleName == "Editor"));
        if (!canEdit)
        {
            // Admin hệ thống vẫn được tạo trong workspace mà họ là member.
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;
            var isMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
            if (isMember && IsAdmin(user)) return user;
            return null;
        }

        return await _context.Users.FindAsync(userId);
    }

    private static BespokeMeta ParseMeta(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new BespokeMeta();
        try
        {
            return JsonSerializer.Deserialize<BespokeMeta>(json, JsonOptions) ?? new BespokeMeta();
        }
        catch
        {
            return new BespokeMeta();
        }
    }

    private static string StatusLabel(string? status) => status?.ToLowerInvariant() switch
    {
        "pending_payment" => "Chờ thanh toán",
        "gathering_data" => "Đang cào & xuất báo cáo",
        "report_ready" => "Báo cáo sẵn sàng",
        "awaiting_reporter" => "Chờ Reporter nhận",
        "pending" => "Chờ Reporter nhận",
        "assigned" => "Cần chỉnh sửa",
        "quoted" => "Chờ khách chấp nhận báo giá",
        "quote_rejected" => "Khách từ chối báo giá",
        "in_progress" => "Đang xử lý",
        "completed" => "Đã nhận báo cáo từ Reporter",
        "revision_requested" => "Cần chỉnh sửa",
        "cancelled" => "Đã hủy",
        _ => "Chờ xử lý"
    };

    private static List<string> DefaultModules() =>
        ["overview", "sentiment", "channel", "influencers", "aspects"];

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static int? ComputePostedSinceDays(BespokeMeta meta)
    {
        var from = ParseDate(meta.DateFrom);
        if (from == null) return null;

        var days = (int)Math.Ceiling((DateTime.Now.Date - from.Value.Date).TotalDays);
        return days > 0 ? days : 1;
    }

    private static string GetBespokeFolder(int requestId) =>
        Path.Combine(AppContext.BaseDirectory, "StorageData", "bespoke", requestId.ToString());

    private static string ResolveFilePath(string stored)
    {
        if (Path.IsPathRooted(stored) && File.Exists(stored)) return stored;
        var relative = Path.Combine(AppContext.BaseDirectory, stored);
        return File.Exists(relative) ? relative : stored;
    }

    private static void DeleteDeliverableFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            var path = ResolveFilePath(relativePath);
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // best-effort cleanup — không chặn luồng chính nếu xoá file lỗi
        }
    }

    public static string GetDeliverableContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".html" or ".htm" => "text/html; charset=utf-8",
            _ => "application/octet-stream"
        };

    // ——— Portal Admin / Reporter (không yêu cầu workspace member) ———

    public async Task<List<PortalBespokeRequestDto>> ListPortalRequestsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new();

        var requests = await LoadAllRequestsWithNavigationsAsync();
        if (IsAdmin(user))
            return await MapPortalListAsync(requests);

        if (!IsReporter(user)) return new();

        // Reporter thấy: đơn đã giao cho mình + đơn khách vừa gửi Reporter, chưa ai nhận.
        var mine = requests.Where(r =>
            r.ReporterId == userId ||
            (r.ReporterId == null && string.Equals(r.Status, "awaiting_reporter", StringComparison.OrdinalIgnoreCase))
        ).ToList();
        return await MapPortalListAsync(mine);
    }

    public async Task<PortalBespokeRequestDto?> GetPortalRequestAsync(int userId, int requestId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        if (!CanViewPortalRequest(user, request)) return null;
        return await MapPortalRequestAsync(request);
    }

    public async Task<BespokeRequestItemDto?> AssignReporterGlobalAsync(
        int adminUserId, int requestId, int reporterId)
    {
        var admin = await _context.Users.FindAsync(adminUserId);
        if (admin == null || !IsAdmin(admin)) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        var reporter = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == reporterId && u.SystemRole == "Reporter");
        if (reporter == null) return null;

        request.ReporterId = reporterId;
        request.AssignedBy = adminUserId;
        request.AssignedAt = DateTime.Now;
        request.Status = "assigned";
        await _context.SaveChangesAsync();

        var ctx = ParseMeta(request.CustomMetrics);
        if (ctx.WorkspaceId > 0 && ctx.ProjectId > 0)
            await EnsureSystemDraftAsync(ctx.WorkspaceId, ctx.ProjectId, adminUserId, requestId);

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, admin);
    }

    public async Task<BespokeRequestItemDto?> QuoteRequestAsync(
        int userId, int requestId, QuoteBespokeDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !IsReporter(user)) return null;

        var request = await GetRequestWithNavigationsAsync(requestId);
        if (request == null) return null;

        var status = request.Status?.ToLowerInvariant();
        if (status is not ("pending" or "assigned" or "quoted")) return null;
        if (request.ReporterId.HasValue && request.ReporterId != userId) return null;

        request.AgreedPrice = dto.AgreedPrice;
        if (dto.Deadline.HasValue)
        {
            var today = DateTime.Today;
            if (dto.Deadline.Value.Date < today)
                return null;
            request.Deadline = dto.Deadline.Value.Date;
        }
        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            request.Requirements = string.IsNullOrWhiteSpace(request.Requirements)
                ? $"[Báo giá Reporter]: {dto.Note.Trim()}"
                : $"{request.Requirements}\n\n[Báo giá Reporter]: {dto.Note.Trim()}";
        }

        request.Status = "quoted";
        if (!request.ReporterId.HasValue) request.ReporterId = userId;
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> AcceptQuoteAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null || IsAdmin(user) || IsReporter(user)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.Status != "quoted") return null;
        if (request.ClientId != userId) return null;

        request.Status = "assigned";
        if (!request.AssignedAt.HasValue) request.AssignedAt = DateTime.Now;

        if (request.AgreedPrice.HasValue && request.AgreedPrice.Value > 0)
        {
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.RequestId == request.RequestId && p.Type == "bespoke");
            if (existingPayment == null)
            {
                _context.Payments.Add(new Payment
                {
                    TransactionRef = $"BESPOKE-{request.RequestId}",
                    Amount = request.AgreedPrice.Value,
                    Status = "success",
                    Type = "bespoke",
                    RequestId = request.RequestId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> RejectQuoteAsync(
        int workspaceId, int projectId, int userId, int requestId)
    {
        var user = await GetUserWithAccessAsync(workspaceId, projectId, userId);
        if (user == null || IsAdmin(user) || IsReporter(user)) return null;

        var request = await GetProjectRequestAsync(projectId, requestId);
        if (request == null || request.Status != "quoted") return null;
        if (request.ClientId != userId) return null;

        request.Status = "quote_rejected";
        await _context.SaveChangesAsync();

        await LoadRequestNavigationsAsync(request);
        return MapRequest(request, user);
    }

    public async Task<BespokeRequestItemDto?> StartWorkByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await StartWorkAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    public async Task<BespokeRequestItemDto?> DeliverReportByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await DeliverReportAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    public async Task<(byte[] Content, string FileName)?> DownloadByRequestIdAsync(int userId, int requestId)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;

        var user = await _context.Users.FindAsync(userId);
        var request = await GetRequestWithNavigationsAsync(requestId);
        if (user != null && request != null && IsReporter(user))
        {
            var st = request.Status?.ToLowerInvariant();

            // Đơn awaiting_reporter chưa giao → Reporter nhận việc + tạo bản nháp hệ thống.
            if (st == "awaiting_reporter" && request.ReporterId == null)
            {
                request.ReporterId = userId;
                request.AssignedAt = DateTime.Now;
                request.Status = "assigned";
                await _context.SaveChangesAsync();
                await EnsureSystemDraftAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
                request = await GetRequestWithNavigationsAsync(requestId);
                st = "assigned";
            }

            if (request != null && request.ReporterId == userId && st is "assigned" or "revision_requested")
            {
                request.Status = "in_progress";
                await _context.SaveChangesAsync();
            }
        }

        return await DownloadDeliverableAsync(meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId);
    }

    public async Task<BespokeRequestItemDto?> UploadRevisionByRequestIdAsync(
        int userId, int requestId, Stream fileStream, string fileName)
    {
        var meta = await ResolveRequestContextAsync(requestId);
        if (meta == null) return null;
        return await UploadRevisionAsync(
            meta.Value.WorkspaceId, meta.Value.ProjectId, userId, requestId, fileStream, fileName);
    }

    private async Task<(int WorkspaceId, int ProjectId)?> ResolveRequestContextAsync(int requestId)
    {
        var request = await _context.BespokeRequests.FindAsync(requestId);
        if (request == null) return null;
        var meta = ParseMeta(request.CustomMetrics);
        if (meta.ProjectId <= 0 || meta.WorkspaceId <= 0) return null;
        return (meta.WorkspaceId, meta.ProjectId);
    }

    private async Task<BespokeRequest?> GetRequestWithNavigationsAsync(int requestId) =>
        await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

    private async Task<List<BespokeRequest>> LoadAllRequestsWithNavigationsAsync() =>
        await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .OrderByDescending(r => r.RequestId)
            .ToListAsync();

    private static bool CanViewPortalRequest(User user, BespokeRequest request) =>
        IsAdmin(user) ||
        (IsReporter(user) && (
            request.ReporterId == user.UserId ||
            (request.ReporterId == null &&
             string.Equals(request.Status, "awaiting_reporter", StringComparison.OrdinalIgnoreCase))
        ));

    private async Task<List<PortalBespokeRequestDto>> MapPortalListAsync(List<BespokeRequest> requests)
    {
        var result = new List<PortalBespokeRequestDto>();
        foreach (var r in requests)
            result.Add(await MapPortalRequestAsync(r));
        return result;
    }

    private async Task<PortalBespokeRequestDto> MapPortalRequestAsync(BespokeRequest r)
    {
        var meta = ParseMeta(r.CustomMetrics);
        var latestReport = r.BespokeReports.OrderByDescending(b => b.UploadedAt).FirstOrDefault();

        string? projectName = null;
        string? workspaceName = null;
        if (meta.ProjectId > 0)
        {
            var project = await _context.Projects.FindAsync(meta.ProjectId);
            projectName = project?.Name;
        }
        if (meta.WorkspaceId > 0)
        {
            var ws = await _context.Workspaces.FindAsync(meta.WorkspaceId);
            workspaceName = ws?.Name;
        }

        return new PortalBespokeRequestDto
        {
            RequestId = r.RequestId,
            Title = r.Title,
            Requirements = r.Requirements,
            Status = r.Status ?? "pending",
            StatusLabel = StatusLabel(r.Status),
            Deadline = r.Deadline,
            SubmittedAt = r.SubmittedAt,
            AssignedAt = r.AssignedAt,
            ClientName = r.Client?.FullName,
            ReporterName = r.Reporter?.FullName,
            ReporterId = r.ReporterId,
            WorkspaceId = meta.WorkspaceId,
            ProjectId = meta.ProjectId,
            ProjectName = projectName,
            WorkspaceName = workspaceName,
            Modules = meta.Modules,
            DateFrom = meta.DateFrom,
            DateTo = meta.DateTo,
            AgreedPrice = r.AgreedPrice,
            HasDeliverable = latestReport != null,
            DeliverableReportId = latestReport?.ReportId,
            RevisionFeedback = meta.RevisionFeedback,
            Keyword = meta.Keyword,
            PackageType = meta.PackageType
        };
    }

    private sealed class BespokeMeta
    {
        public int ProjectId { get; set; }
        public int WorkspaceId { get; set; }
        public string? Keyword { get; set; }
        public string PackageType { get; set; } = "basic";
        public decimal? PackagePrice { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public List<string> Modules { get; set; } = new();
        public string Format { get; set; } = "pdf";
        public string? RevisionFeedback { get; set; }
        public string? ScrapeJobId { get; set; }
        public string? PreviousSearchQuery { get; set; }
        public DateTime? ScrapeStartedAt { get; set; }
    }

    private static string NormalizePackageType(string? packageType) =>
        packageType?.Trim().ToLowerInvariant() == "pro" ? "pro" : "basic";

    private static bool IsAllowedDeliverableExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".html" or ".htm" or ".pdf" or ".pptx" or ".ppt";
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "report.bin" : name;
    }
}
