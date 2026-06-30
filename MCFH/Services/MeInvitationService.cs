using MCFH.DTOs.WorkspaceDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// Lời mời workspace nhận được — tách riêng để không sửa WorkspaceService hiện có.
/// </summary>
public class MeInvitationService
{
    private readonly McfhDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private const string FrontendBaseUrl = "http://localhost:5173";

    public MeInvitationService(
        McfhDbContext context,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<List<ReceivedInvitationDto>> GetMyReceivedInvitationsAsync(int userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return [];

        var email = user.Email.Trim().ToLowerInvariant();

        return await _context.WorkspaceInvitations
            .Where(i => i.Status == "pending" && i.InvitedEmail.ToLower() == email)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ReceivedInvitationDto
            {
                InvitationId = i.InvitationId,
                WorkspaceId = i.WorkspaceId,
                WorkspaceName = i.Workspace.Name,
                InvitedByName = i.InvitedByNavigation.FullName,
                Status = i.Status ?? "pending",
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> AcceptInvitationAsync(int userId, int invitationId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        var invitation = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId && i.Status == "pending");

        if (invitation == null) return false;
        if (!string.Equals(invitation.InvitedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            return false;

        var alreadyMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == userId);

        if (!alreadyMember)
        {
            var viewerRole = await _context.WorkspaceRoles.FirstAsync(r => r.RoleName == "Viewer");
            _context.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = invitation.WorkspaceId,
                UserId = user.UserId,
                RoleId = viewerRole.RoleId
            });
        }

        invitation.Status = "accepted";
        await _context.SaveChangesAsync();

        await _notificationService.MarkRelatedReadAsync(userId, "workspace_invitation", invitationId);
        await _notificationService.NotifyAsync(
            invitation.InvitedBy,
            $"{user.FullName} đã chấp nhận lời mời",
            $"{user.FullName} đã tham gia workspace \"{invitation.Workspace.Name}\".",
            "invitation_accepted",
            "workspace_invitation",
            invitationId);

        return true;
    }

    public async Task<bool> DeclineInvitationAsync(int userId, int invitationId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        var invitation = await _context.WorkspaceInvitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.InvitationId == invitationId && i.Status == "pending");

        if (invitation == null) return false;
        if (!string.Equals(invitation.InvitedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            return false;

        invitation.Status = "rejected";
        await _context.SaveChangesAsync();

        await _notificationService.MarkRelatedReadAsync(userId, "workspace_invitation", invitationId);
        await _notificationService.NotifyAsync(
            invitation.InvitedBy,
            $"{user.FullName} đã từ chối lời mời",
            $"{user.FullName} không tham gia workspace \"{invitation.Workspace.Name}\".",
            "invitation_declined",
            "workspace_invitation",
            invitationId);

        return true;
    }
}
