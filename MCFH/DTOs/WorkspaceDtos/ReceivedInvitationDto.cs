namespace MCFH.DTOs.WorkspaceDtos;

public class ReceivedInvitationDto
{
    public int InvitationId { get; set; }
    public int WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = "";
    public string InvitedByName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
}
