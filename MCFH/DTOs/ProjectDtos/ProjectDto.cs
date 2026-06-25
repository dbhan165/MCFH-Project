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
        public bool EnableFacebook { get; set; }
        public bool EnableYoutube { get; set; }
        public bool EnableTiktok { get; set; }
        public bool EnableMaps { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateDataSourceDto
    {
        public string Platform { get; set; } = null!;
        public string? TargetUrl { get; set; }
    }

    public class CreateProjectDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SearchQuery { get; set; }
        public bool EnableFacebook { get; set; }
        public bool EnableYoutube { get; set; }
        public bool EnableTiktok { get; set; }
        public bool EnableMaps { get; set; }
        public List<CreateDataSourceDto> DataSources { get; set; } = new();
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