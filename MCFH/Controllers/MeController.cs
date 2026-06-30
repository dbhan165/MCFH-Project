using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly MeInvitationService _invitationService;
    private readonly INotificationService _notificationService;

    public MeController(McfhDbContext db, IEmailService emailService)
    {
        var notifications = new NotificationService(db);
        _notificationService = notifications;
        _invitationService = new MeInvitationService(db, notifications, emailService);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("invitations")]
    public async Task<IActionResult> GetMyInvitations() =>
        Ok(await _invitationService.GetMyReceivedInvitationsAsync(GetUserId()));

    [HttpPost("invitations/{invitationId}/accept")]
    public async Task<IActionResult> AcceptInvitation(int invitationId)
    {
        var success = await _invitationService.AcceptInvitationAsync(GetUserId(), invitationId);
        if (!success) return BadRequest(new { message = "Không thể chấp nhận lời mời." });
        return Ok(new { message = "Đã tham gia workspace thành công." });
    }

    [HttpPost("invitations/{invitationId}/decline")]
    public async Task<IActionResult> DeclineInvitation(int invitationId)
    {
        var success = await _invitationService.DeclineInvitationAsync(GetUserId(), invitationId);
        if (!success) return BadRequest(new { message = "Không thể từ chối lời mời." });
        return Ok(new { message = "Đã từ chối lời mời." });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 30) =>
        Ok(await _notificationService.GetRecentAsync(GetUserId(), limit));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadCount() =>
        Ok(new UnreadCountDto { Count = await _notificationService.GetUnreadCountAsync(GetUserId()) });

    [HttpPut("notifications/{notificationId}/read")]
    public async Task<IActionResult> MarkNotificationRead(int notificationId)
    {
        var success = await _notificationService.MarkReadAsync(GetUserId(), notificationId);
        if (!success) return NotFound();
        return Ok(new { message = "Đã đánh dấu đã đọc." });
    }

    [HttpPut("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        await _notificationService.MarkAllReadAsync(GetUserId());
        return Ok(new { message = "Đã đánh dấu tất cả đã đọc." });
    }
}
