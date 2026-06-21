using MCFH.DTOs.WorkspaceDtos;

namespace MCFH.Services
{
    public interface IWorkspaceService
    {
        // Workspace CRUD
        Task<List<WorkspaceResponseDto>> GetMyWorkspacesAsync(int userId);
        Task<WorkspaceResponseDto?> GetByIdAsync(int workspaceId, int userId);
        Task<WorkspaceResponseDto> CreateAsync(int userId, CreateWorkspaceDto dto);
        Task<WorkspaceResponseDto?> UpdateAsync(int workspaceId, int userId, UpdateWorkspaceDto dto);
        Task<bool> DeleteAsync(int workspaceId, int userId);

        // Member Management
        Task<List<MemberResponseDto>> GetMembersAsync(int workspaceId, int userId);
        Task<bool> InviteMemberAsync(int workspaceId, int invitedByUserId, InviteMemberDto dto);
        Task<List<InvitationResponseDto>> GetPendingInvitationsAsync(int workspaceId, int ownerId);
        Task<bool> ApproveInvitationAsync(int workspaceId, int ownerId, int invitationId);
        Task<bool> RejectInvitationAsync(int workspaceId, int ownerId, int invitationId);
        Task<bool> UpdateMemberRoleAsync(int workspaceId, int ownerId, int targetUserId, UpdateMemberRoleDto dto);
        Task<bool> KickMemberAsync(int workspaceId, int ownerId, int targetUserId);
        // Activity Log
        Task<List<ActivityLogResponseDto>> GetActivityLogsAsync(int workspaceId, int userId, int page = 1, int pageSize = 20);
    }
}