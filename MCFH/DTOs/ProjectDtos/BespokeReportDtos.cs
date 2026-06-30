namespace MCFH.DTOs.ProjectDtos;

public class CreateBespokeRequestDto
{
    public string Title { get; set; } = null!;
    public string? Requirements { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public List<string> Modules { get; set; } = new();
    public string Format { get; set; } = "html";
}

public class AssignBespokeReporterDto
{
    public int ReporterId { get; set; }
}

public class BespokeRequestItemDto
{
    public int RequestId { get; set; }
    public string Title { get; set; } = null!;
    public string? Requirements { get; set; }
    public string Status { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public DateTime? Deadline { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ClientName { get; set; }
    public string? ReporterName { get; set; }
    public int? ReporterId { get; set; }
    public List<string> Modules { get; set; } = new();
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string Format { get; set; } = "html";
    public bool HasDeliverable { get; set; }
    public int? DeliverableReportId { get; set; }
}

public class ReporterOptionDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}

public class BespokeCenterDto
{
    public string UserSystemRole { get; set; } = null!;
    public List<BespokeRequestItemDto> Requests { get; set; } = new();
    public List<ReporterOptionDto> Reporters { get; set; } = new();
}
