namespace MCFH.DTOs.ProjectDtos;

public class ReportTemplateDto
{
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Format { get; set; } = null!;
    public string TypeLabel { get; set; } = null!;
}

public class ReportFileDto
{
    public string ReportId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string TypeLabel { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string Status { get; set; } = "ready";
    public long FileSizeBytes { get; set; }
    public int RowCount { get; set; }
}

public class ReportCenterDto
{
    public int TotalReports { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public List<ReportTemplateDto> Templates { get; set; } = new();
    public List<ReportFileDto> Reports { get; set; } = new();
}

public class GenerateReportRequestDto
{
    public string Type { get; set; } = null!;
}
