namespace MCFH.DTOs;

public class MentionQueryDto
{
    public string? Platform { get; set; }
    public string? Sentiment { get; set; }
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool ExcludeMuted { get; set; } = true;
    public bool? IsCrisisAlert { get; set; }
}

public class SavedFilterDto
{
    public int FilterId { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public MentionQueryDto Config { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
}

public class CreateSavedFilterDto
{
    public string Name { get; set; } = null!;
    public MentionQueryDto Config { get; set; } = new();
}

public class UpdateSavedFilterDto
{
    public string Name { get; set; } = null!;
    public MentionQueryDto Config { get; set; } = new();
}
