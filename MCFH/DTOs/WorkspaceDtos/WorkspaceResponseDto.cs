namespace MCFH.DTOs.WorkspaceDtos
{
    public class WorkspaceResponseDto
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = null!;
        public string OwnerName { get; set; } = null!;
        public string? MyRole { get; set; }
        public int MemberCount { get; set; }
        public int ProjectCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateWorkspaceDto
    {
        public string Name { get; set; } = null!;
    }

    public class UpdateWorkspaceDto
    {
        public string Name { get; set; } = null!;
    }
}