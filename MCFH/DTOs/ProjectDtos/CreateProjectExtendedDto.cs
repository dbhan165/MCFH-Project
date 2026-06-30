namespace MCFH.DTOs.ProjectDtos;

public class CreateDataSourceDto
{
    public string Platform { get; set; } = null!;
    public string? TargetUrl { get; set; }
}

public class CreateProjectExtendedDto
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

public class ProjectExtendedResponseDto
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

public class UpdateProjectExtendedDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? SearchQuery { get; set; }
    public bool? EnableFacebook { get; set; }
    public bool? EnableYoutube { get; set; }
    public bool? EnableTiktok { get; set; }
    public bool? EnableMaps { get; set; }
}
