using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AdminPortalService _admin;
    private readonly SubscriptionService _subscription;

    public AdminController(AdminPortalService admin, SubscriptionService subscription)
    {
        _admin = admin;
        _subscription = subscription;
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

    [HttpPatch("users/{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateAdminUserDto dto)
    {
        var result = await _admin.UpdateUserAsync(GetUserId(), userId, dto);
        if (result == null) return NotFound(new { message = "Không tìm thấy user hoặc không có quyền." });
        return Ok(result);
    }

    [HttpGet("bespoke")]
    public async Task<IActionResult> ListBespoke()
    {
        var result = await _admin.ListBespokeRequestsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("reporters")]
    public async Task<IActionResult> ListReporters()
    {
        var result = await _admin.ListReportersAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("bespoke/{requestId}/assign")]
    public async Task<IActionResult> AssignReporter(int requestId, [FromBody] AssignBespokeGlobalDto dto)
    {
        var result = await _admin.AssignReporterAsync(GetUserId(), requestId, dto.ReporterId);
        if (result == null) return BadRequest(new { message = "Không thể giao Reporter." });
        return Ok(result);
    }

    [HttpGet("subscription-plans")]
    public async Task<IActionResult> ListSubscriptionPlans()
    {
        var result = await _subscription.GetAdminPlansAsync(GetUserId());
        return Ok(result);
    }

    [HttpPut("subscription-plans/{planId}")]
    public async Task<IActionResult> UpdateSubscriptionPlan(int planId, [FromBody] UpdateSubscriptionPlanDto dto)
    {
        var result = await _subscription.UpdatePlanAsync(GetUserId(), planId, dto);
        if (result == null) return BadRequest(new { message = "Không thể cập nhật gói cước." });
        return Ok(result);
    }

    [HttpGet("proxies")]
    public async Task<IActionResult> ListProxies() => Ok(await _admin.ListProxiesAsync(GetUserId()));

    [HttpPost("proxies")]
    public async Task<IActionResult> CreateProxy([FromBody] UpsertSystemProxyDto dto)
    {
        var result = await _admin.CreateProxyAsync(GetUserId(), dto);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpPut("proxies/{proxyId}")]
    public async Task<IActionResult> UpdateProxy(int proxyId, [FromBody] UpsertSystemProxyDto dto)
    {
        var result = await _admin.UpdateProxyAsync(GetUserId(), proxyId, dto);
        if (result == null) return BadRequest(new { message = "Không thể cập nhật proxy." });
        return Ok(result);
    }

    [HttpDelete("proxies/{proxyId}")]
    public async Task<IActionResult> DeleteProxy(int proxyId)
    {
        var ok = await _admin.DeleteProxyAsync(GetUserId(), proxyId);
        if (!ok) return BadRequest(new { message = "Không thể xóa proxy." });
        return Ok(new { message = "Đã xóa proxy." });
    }

    [HttpGet("scraping-jobs")]
    public async Task<IActionResult> ListScrapingJobs() => Ok(await _admin.ListScrapingJobsAsync(GetUserId()));

    [HttpGet("settings")]
    public async Task<IActionResult> ListSettings() => Ok(await _admin.ListSettingsAsync(GetUserId()));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsDto dto)
    {
        var result = await _admin.UpdateSettingsAsync(GetUserId(), dto);
        if (result.Count == 0) return Forbid();
        return Ok(result);
    }
}
