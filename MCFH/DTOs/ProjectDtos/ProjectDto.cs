namespace MCFH.DTOs.ProjectDtos
{
    public class ProjectResponseDto
    {
        public int ProjectId { get; set; }
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SearchQuery { get; set; }
        public int DataSourceCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateProjectDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class UpdateProjectDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class AnalyzeProjectResultDto
    {
        public int ProjectId { get; set; }
        public int AnalyzedCount { get; set; }
        public int SkippedCount { get; set; }
        public int TotalFeedbacks { get; set; }
        public string Message { get; set; } = null!;
    }
}