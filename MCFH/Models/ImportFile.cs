using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class ImportFile
{
    public int FileId { get; set; }

    public int ProjectId { get; set; }

    public int? SourceId { get; set; }

    public int UploadedBy { get; set; }

    public string FileName { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public int? TotalRows { get; set; }

    public int? ImportedRows { get; set; }

    public string? Status { get; set; }

    public DateTime? ImportedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual DataSource? Source { get; set; }

    public virtual User UploadedByNavigation { get; set; } = null!;
}
