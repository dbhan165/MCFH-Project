using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services;
using MCFH.Services.Scraping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

/// <summary>
/// Admin dashboard, users, bespoke, subscription plans, settings.
/// Proxy/scraping endpoints remain in AdminProxyController.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminPortalController : ControllerBase
{
    private readonly AdminPortalService _admin;
    private readonly SubscriptionService _subscription;

    public AdminPortalController(McfhDbContext db, IEmailService emailService, ICommentBundleStorage bundleStorage, EncryptionService encryption, IAiSentimentService aiSentiment, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        var analytics = new ProjectAnalyticsService(db, bundleStorage);
        var reports = new ProjectReportService(db, analytics, aiSentiment);
        var bespoke = new BespokeReportService(db, analytics, emailService, reportService: reports);
        _admin = new AdminPortalService(db, bespoke, encryption, cache);
        _subscription = new SubscriptionService(db);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _admin.GetDashboardAsync(GetUserId());
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _admin.ListUsersAsync(GetUserId(), search, role, page, pageSize);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserDetail(int userId)
    {
        var result = await _admin.GetUserDetailAsync(GetUserId(), userId);
        if (result == null) return NotFound(new { message = "Không tìm thấy user hoặc không có quyền." });
        return Ok(result);
    }

    [HttpPatch("users/{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateAdminUserDto dto)
    {
        var result = await _admin.UpdateUserAsync(GetUserId(), userId, dto);
        if (result == null) return NotFound(new { message = "Không tìm thấy user hoặc không có quyền." });
        return Ok(result);
    }

    [HttpGet("bespoke")]
    public async Task<IActionResult> ListBespoke() =>
        Ok(await _admin.ListBespokeRequestsAsync(GetUserId()));

    [HttpGet("reporters")]
    public async Task<IActionResult> ListReporters() =>
        Ok(await _admin.ListReportersAsync(GetUserId()));

    [HttpPost("bespoke/{requestId}/assign")]
    public async Task<IActionResult> AssignReporter(int requestId, [FromBody] AssignBespokeGlobalDto dto)
    {
        var result = await _admin.AssignReporterAsync(GetUserId(), requestId, dto.ReporterId);
        if (result == null) return BadRequest(new { message = "Không thể giao Reporter." });
        return Ok(result);
    }

    [HttpGet("subscription-plans")]
    public async Task<IActionResult> ListSubscriptionPlans() =>
        Ok(await _subscription.GetAdminPlansAsync(GetUserId()));

    [HttpPut("subscription-plans/{planId}")]
    public async Task<IActionResult> UpdateSubscriptionPlan(int planId, [FromBody] UpdateSubscriptionPlanDto dto)
    {
        var result = await _subscription.UpdatePlanAsync(GetUserId(), planId, dto);
        if (result == null) return BadRequest(new { message = "Không thể cập nhật gói cước." });
        return Ok(result);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> ListSettings() =>
        Ok(await _admin.ListSettingsAsync(GetUserId()));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsDto dto)
    {
        var result = await _admin.UpdateSettingsAsync(GetUserId(), dto);
        if (result.Count == 0) return Forbid();
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 50) =>
        Ok(await _admin.GetAuditLogsAsync(GetUserId(), limit));

    [HttpGet("nsr-config")]
    public IActionResult GetNsrConfig()
    {
        return Ok(new
        {
            formula = "NSR = [(WeightedPositive - WeightedNegative) / TotalWeight] * 100%",
            description = "Hệ thống tự động tính điểm Weighted NSR nhằm nâng cao độ uy tín bằng cách nhân trọng số cho bài viết từ KOLs/Hot posts hoặc các nguồn truyền thông lớn.",
            status = "Active",
            rules = new[]
            {
                new { name = "Bài viết / Comment tương tác cao (>= 20 likes/replies)", weight = $"{NsrCalculator.HIGH_ENGAGEMENT_MULTIPLIER}x", description = "Bài viết có độ phủ lớn hoặc từ KOLs" },
                new { name = "Nguồn tin tức (News / Báo chí)", weight = $"{NsrCalculator.NEWS_PLATFORM_MULTIPLIER}x", description = "Tác động thương hiệu chính thống lâu dài" },
                new { name = "Nguồn truyền thông & Mạng xã hội", weight = $"{NsrCalculator.MEDIA_PLATFORM_MULTIPLIER}x", description = "Đề cập trên các nền tảng MXH (Facebook, TikTok, YouTube, Threads,...)" },
                new { name = "Mention thông thường", weight = $"{NsrCalculator.DEFAULT_WEIGHT}x", description = "Bình luận tiêu chuẩn" }
            }
        });
    }
}
