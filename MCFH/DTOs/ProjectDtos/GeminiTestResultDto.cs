namespace MCFH.DTOs.ProjectDtos;

public class GeminiTestResultDto
{
    public bool Configured { get; set; }
    public bool Success { get; set; }
    public string? ModelUsed { get; set; }
    public string Message { get; set; } = "";
    public string? SampleSummary { get; set; }
    public string? SampleSentiment { get; set; }
}
