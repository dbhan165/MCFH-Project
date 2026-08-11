using System;
using System.ComponentModel.DataAnnotations;

namespace MCFH.DTOs.ProjectDtos;

public class DataSourceDto
{
    public int SourceId { get; set; }
    public string Platform { get; set; } = null!;
    public string SourceType { get; set; } = null!;
    public string? TargetUrl { get; set; }
    public string? SearchQuery { get; set; }
    public string? Status { get; set; }
}

public class CreateProjectDataSourceDto
{
    [Required]
    public string Platform { get; set; } = null!;
    [Required]
    public string SourceType { get; set; } = null!; // e.g. "keyword", "page", "group"
    public string? TargetUrl { get; set; }
    public string? SearchQuery { get; set; }
}

public class ImportFileDto
{
    public int FileId { get; set; }
    public int? SourceId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public int? TotalRows { get; set; }
    public int? ImportedRows { get; set; }
    public string? Status { get; set; }
    public DateTime? ImportedAt { get; set; }
    public string UploadedByName { get; set; } = null!;
}

public class CreateImportFileDto
{
    public int? SourceId { get; set; }
    [Required]
    public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;
}
