using MCFH.DTOs.WorkspaceDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly McfhDbContext _context;
        private readonly IEmailService _emailService;

        public WorkspaceService(McfhDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── Helper: kiểm tra user có phải Owner không ──
        private async Task<bool> IsOwnerAsync(int workspaceId, int userId)
        {
            return await _context.Workspaces
                .AnyAsync(w => w.WorkspaceId == workspaceId &&
                               w.OwnerId == userId &&
                               w.IsDeleted != true);
        }

        // ── Helper: kiểm tra user có phải member không ──
        private async Task<bool> IsMemberAsync(int workspaceId, int userId)
        {
            return await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId &&
                               m.UserId == userId);
        }

        // ── Helper: ghi activity log ──
        private async Task LogActivityAsync(int workspaceId, int userId, string actionType,
            string? targetType = null, int? targetId = null,
            string? targetName = null, string? description = null)
        {
            _context.WorkspaceActivityLogs.Add(new WorkspaceActivityLog
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                ActionType = actionType,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                Description = description,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        // ════════════════════════════════
        // WORKSPACE CRUD
        // ════════════════════════════════

        public async Task<List<WorkspaceResponseDto>> GetMyWorkspacesAsync(int userId)
        {
            return await _context.Workspaces
                .Where(w => w.IsDeleted != true &&
                            w.WorkspaceMembers.Any(m => m.UserId == userId))
                .Select(w => new WorkspaceResponseDto
                {
                    WorkspaceId = w.WorkspaceId,
                    Name = w.Name,
                    OwnerName = w.Owner.FullName,
                    MemberCount = w.WorkspaceMembers.Count,
                    ProjectCount = w.Projects.Count(p => p.IsDeleted != true),
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<WorkspaceResponseDto?> GetByIdAsync(int workspaceId, int userId)
        {
            return await _context.Workspaces
                .Where(w => w.WorkspaceId == workspaceId &&
                            w.IsDeleted != true &&
                            w.WorkspaceMembers.Any(m => m.UserId == userId))
                .Select(w => new WorkspaceResponseDto
                {
                    WorkspaceId = w.WorkspaceId,
                    Name = w.Name,
                    OwnerName = w.Owner.FullName,
                    MemberCount = w.WorkspaceMembers.Count,
                    ProjectCount = w.Projects.Count(p => p.IsDeleted != true),
                    CreatedAt = w.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<WorkspaceResponseDto> CreateAsync(int userId, CreateWorkspaceDto dto)
        {
            var ownerRole = await _context.WorkspaceRoles
                .FirstAsync(r => r.RoleName == "Owner");

            var workspace = new Workspace
            {
                Name = dto.Name,
                OwnerId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            _context.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace.WorkspaceId,
                UserId = userId,
                RoleId = ownerRole.RoleId
            });

            _context.WorkspaceCredits.Add(new WorkspaceCredit
            {
                WorkspaceId = workspace.WorkspaceId,
                TotalCredits = 0,
                UsedCredits = 0
            });

            await _context.SaveChangesAsync();

            await LogActivityAsync(workspace.WorkspaceId, userId,
                actionType: "CREATE_WORKSPACE",
                targetType: "workspace",
                targetId: workspace.WorkspaceId,
                targetName: workspace.Name,
                description: $"Tạo workspace \"{workspace.Name}\"");

            return await GetByIdAsync(workspace.WorkspaceId, userId)
                   ?? throw new Exception("Tạo workspace thất bại");
        }

        public async Task<WorkspaceResponseDto?> UpdateAsync(int workspaceId, int userId, UpdateWorkspaceDto dto)
        {
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId &&
                                          w.OwnerId == userId &&
                                          w.IsDeleted != true);

            if (workspace == null) return null;

            var oldName = workspace.Name;
            workspace.Name = dto.Name;
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, userId,
                actionType: "UPDATE_WORKSPACE",
                targetType: "workspace",
                targetId: workspaceId,
                targetName: dto.Name,
                description: $"Đổi tên workspace từ \"{oldName}\" thành \"{dto.Name}\"");

            return await GetByIdAsync(workspaceId, userId);
        }

        public async Task<bool> DeleteAsync(int workspaceId, int userId)
        {
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId &&
                                          w.OwnerId == userId &&
                                          w.IsDeleted != true);

            if (workspace == null) return false;

            workspace.IsDeleted = true;
            workspace.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, userId,
                actionType: "DELETE_WORKSPACE",
                targetType: "workspace",
                targetId: workspaceId,
                targetName: workspace.Name,
                description: $"Xóa workspace \"{workspace.Name}\"");

            return true;
        }

        // ════════════════════════════════
        // MEMBER MANAGEMENT
        // ════════════════════════════════

        public async Task<List<MemberResponseDto>> GetMembersAsync(int workspaceId, int userId)
        {
            if (!await IsMemberAsync(workspaceId, userId))
                return new List<MemberResponseDto>();

            return await _context.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId)
                .Select(m => new MemberResponseDto
                {
                    UserId = m.User.UserId,
                    FullName = m.User.FullName,
                    Email = m.User.Email,
                    AvatarUrl = m.User.AvatarUrl,
                    RoleName = m.Role.RoleName
                })
                .ToListAsync();
        }

        public async Task<bool> InviteMemberAsync(int workspaceId, int invitedByUserId, InviteMemberDto dto)
        {
            if (!await IsMemberAsync(workspaceId, invitedByUserId))
                return false;

            var existing = await _context.WorkspaceInvitations
                .AnyAsync(i => i.WorkspaceId == workspaceId &&
                               i.InvitedEmail == dto.Email &&
                               i.Status == "pending");

            if (existing) return false;

            var alreadyMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId &&
                               m.User.Email == dto.Email);

            if (alreadyMember) return false;

            // Lấy tên workspace để hiển thị trong email
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId);

            // Lấy tên người mời
            var invitedByUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == invitedByUserId);

            _context.WorkspaceInvitations.Add(new WorkspaceInvitation
            {
                WorkspaceId = workspaceId,
                InvitedEmail = dto.Email,
                InvitedBy = invitedByUserId,
                Status = "pending",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Gửi email thông báo
            var subject = $"[MCFH] Bạn được mời vào workspace \"{workspace?.Name}\"";
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>Lời mời tham gia Workspace</h2>
                    <p>Xin chào,</p>
                    <p>
                        <strong>{invitedByUser?.FullName}</strong> đã mời bạn tham gia workspace
                        <strong>'{workspace?.Name}'</strong> trên hệ thống MCFH.
                    </p>
                    <p>Vui lòng đăng nhập vào hệ thống để xem lời mời:</p>
                    <a href='http://localhost:3000/workspaces'
                       style='background-color: #4F46E5; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; display: inline-block;
                              margin: 16px 0;'>
                        Xem lời mời
                    </a>
                    <p style='color: #6B7280; font-size: 14px;'>
                        Lưu ý: Lời mời cần được Owner phê duyệt trước khi bạn có thể tham gia.
                    </p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                    <p style='color: #9CA3AF; font-size: 12px;'>
                        Email này được gửi tự động từ hệ thống MCFH. Vui lòng không reply.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(dto.Email, subject, htmlMessage);

            await LogActivityAsync(workspaceId, invitedByUserId,
                actionType: "INVITE_MEMBER",
                targetType: "member",
                targetName: dto.Email,
                description: $"Mời \"{dto.Email}\" vào workspace");

            return true;
        }

        public async Task<List<InvitationResponseDto>> GetPendingInvitationsAsync(int workspaceId, int ownerId)
        {
            if (!await IsOwnerAsync(workspaceId, ownerId))
                return new List<InvitationResponseDto>();

            return await _context.WorkspaceInvitations
                .Where(i => i.WorkspaceId == workspaceId && i.Status == "pending")
                .Select(i => new InvitationResponseDto
                {
                    InvitationId = i.InvitationId,
                    InvitedEmail = i.InvitedEmail,
                    InvitedByName = i.InvitedByNavigation.FullName,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveInvitationAsync(int workspaceId, int ownerId, int invitationId)
        {
            if (!await IsOwnerAsync(workspaceId, ownerId))
                return false;

            var invitation = await _context.WorkspaceInvitations
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId &&
                                          i.WorkspaceId == workspaceId &&
                                          i.Status == "pending");

            if (invitation == null) return false;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == invitation.InvitedEmail);

            if (user == null) return false;

            var viewerRole = await _context.WorkspaceRoles
                .FirstAsync(r => r.RoleName == "Viewer");

            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId);

            _context.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = user.UserId,
                RoleId = viewerRole.RoleId
            });

            invitation.Status = "accepted";
            await _context.SaveChangesAsync();

            // Gửi email thông báo được duyệt
            var subject = $"[MCFH] Lời mời vào workspace \"{workspace?.Name}\" đã được chấp nhận";
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #10B981;'>Lời mời đã được duyệt!</h2>
                    <p>Xin chào <strong>{user.FullName}</strong>,</p>
                    <p>
                        Lời mời tham gia workspace <strong>'{workspace?.Name}'</strong>
                        của bạn đã được Owner chấp nhận.
                    </p>
                    <p>Bạn có thể đăng nhập ngay để bắt đầu làm việc:</p>
                    <a href='http://localhost:3000/workspaces'
                       style='background-color: #10B981; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; display: inline-block;
                              margin: 16px 0;'>
                        Vào Workspace
                    </a>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                    <p style='color: #9CA3AF; font-size: 12px;'>
                        Email này được gửi tự động từ hệ thống MCFH. Vui lòng không reply.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, subject, htmlMessage);

            await LogActivityAsync(workspaceId, ownerId,
                actionType: "APPROVE_INVITE",
                targetType: "member",
                targetId: user.UserId,
                targetName: user.FullName,
                description: $"Duyệt lời mời cho \"{user.FullName}\"");

            return true;
        }

        public async Task<bool> RejectInvitationAsync(int workspaceId, int ownerId, int invitationId)
        {
            if (!await IsOwnerAsync(workspaceId, ownerId))
                return false;

            var invitation = await _context.WorkspaceInvitations
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId &&
                                          i.WorkspaceId == workspaceId &&
                                          i.Status == "pending");

            if (invitation == null) return false;

            invitation.Status = "rejected";
            await _context.SaveChangesAsync();

            // Gửi email thông báo bị từ chối
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId);

            var subject = $"[MCFH] Lời mời vào workspace \"{workspace?.Name}\" đã bị từ chối";
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #EF4444;'>Lời mời không được chấp nhận</h2>
                    <p>Xin chào,</p>
                    <p>
                        Rất tiếc, lời mời tham gia workspace <strong>'{workspace?.Name}'</strong>
                        của bạn đã bị Owner từ chối.
                    </p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 24px 0;'/>
                    <p style='color: #9CA3AF; font-size: 12px;'>
                        Email này được gửi tự động từ hệ thống MCFH. Vui lòng không reply.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(invitation.InvitedEmail, subject, htmlMessage);

            await LogActivityAsync(workspaceId, ownerId,
                actionType: "REJECT_INVITE",
                targetType: "member",
                targetName: invitation.InvitedEmail,
                description: $"Từ chối lời mời cho \"{invitation.InvitedEmail}\"");

            return true;
        }

        public async Task<bool> UpdateMemberRoleAsync(int workspaceId, int ownerId, int targetUserId, UpdateMemberRoleDto dto)
        {
            if (!await IsOwnerAsync(workspaceId, ownerId))
                return false;

            if (targetUserId == ownerId) return false;

            var member = await _context.WorkspaceMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId &&
                                          m.UserId == targetUserId);

            if (member == null) return false;

            var newRole = await _context.WorkspaceRoles
                .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);

            if (newRole == null) return false;

            member.RoleId = newRole.RoleId;
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, ownerId,
                actionType: "UPDATE_ROLE",
                targetType: "member",
                targetId: targetUserId,
                targetName: member.User.FullName,
                description: $"Đổi role của \"{member.User.FullName}\" thành \"{dto.RoleName}\"");

            return true;
        }

        public async Task<bool> KickMemberAsync(int workspaceId, int ownerId, int targetUserId)
        {
            if (!await IsOwnerAsync(workspaceId, ownerId))
                return false;

            if (targetUserId == ownerId) return false;

            var member = await _context.WorkspaceMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId &&
                                          m.UserId == targetUserId);

            if (member == null) return false;

            var memberName = member.User.FullName;

            _context.WorkspaceMembers.Remove(member);
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, ownerId,
                actionType: "KICK_MEMBER",
                targetType: "member",
                targetId: targetUserId,
                targetName: memberName,
                description: $"Đã kick \"{memberName}\" khỏi workspace");

            return true;
        }

        // ════════════════════════════════
        // ACTIVITY LOG
        // ════════════════════════════════

        public async Task<List<ActivityLogResponseDto>> GetActivityLogsAsync(int workspaceId, int userId, int page = 1, int pageSize = 20)
        {
            if (!await IsMemberAsync(workspaceId, userId))
                return new List<ActivityLogResponseDto>();

            return await _context.WorkspaceActivityLogs
                .Where(l => l.WorkspaceId == workspaceId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ActivityLogResponseDto
                {
                    LogId = l.LogId,
                    ActionType = l.ActionType,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    TargetName = l.TargetName,
                    Description = l.Description,
                    UserFullName = l.User.FullName,
                    UserAvatarUrl = l.User.AvatarUrl,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();
        }
    }
}