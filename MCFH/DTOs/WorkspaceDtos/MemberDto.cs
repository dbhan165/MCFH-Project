namespace MCFH.DTOs.WorkspaceDtos
{
    // Thông tin 1 member
    public class MemberResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string RoleName { get; set; } = null!;
    }

    // Mời thành viên qua email
    public class InviteMemberDto
    {
        public string Email { get; set; } = null!;
    }

    // Đổi role (chỉ Owner)
    public class UpdateMemberRoleDto
    {
        public string RoleName { get; set; } = null!; // 'Editor' | 'Viewer'
    }

    // Thông tin 1 lời mời đang pending
    public class InvitationResponseDto
    {
        public int InvitationId { get; set; }
        public string InvitedEmail { get; set; } = null!;
        public string InvitedByName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}