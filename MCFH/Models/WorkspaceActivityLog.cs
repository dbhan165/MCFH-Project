namespace MCFH.Models
{
    public partial class WorkspaceActivityLog
    {
        public int LogId { get; set; }
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }
        public string ActionType { get; set; } = null!;
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        public virtual Workspace Workspace { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}