using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Subscription;

[ApiController]
[Route("api/subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _subscription;

    public SubscriptionController(McfhDbContext db) => _subscription = new SubscriptionService(db);

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim) : null;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans() => Ok(await _subscription.GetPublicPlansAsync());

    [Authorize]
    [HttpGet("billing")]
    public async Task<IActionResult> GetBilling([FromQuery] int? workspaceId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _subscription.GetBillingSummaryAsync(userId.Value, workspaceId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize]
    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await _subscription.GetPaymentHistoryAsync(userId.Value));
    }

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _subscription.SubscribeAsync(userId.Value, dto);
        if (result == null) return BadRequest();
        return Ok(result);
    }
}
