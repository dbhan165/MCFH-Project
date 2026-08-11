namespace MCFH.DTOs.ProjectDtos;

public class ReportInsightsResultDto
{
    public List<string> ExecutiveInsights { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
}
