namespace MCFH.DTOs.WorkspaceDtos
{
    public class ActivityLogResponseDto
    {
        public int LogId { get; set; }
        public string ActionType { get; set; } = null!;
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? Description { get; set; }
        public string UserFullName { get; set; } = null!;
        public string? UserAvatarUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}