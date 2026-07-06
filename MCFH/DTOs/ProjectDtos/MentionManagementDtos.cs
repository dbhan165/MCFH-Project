namespace MCFH.DTOs.ProjectDtos;

public class MentionTagDto
{
    public int TagId { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}

public class CreateMentionTagDto
{
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}

public class AssignMentionTagsDto
{
    public List<int> TagIds { get; set; } = new();
}

public class UpdateMentionSentimentDto
{
    public string Sentiment { get; set; } = null!;
}

public class MuteEntityDto
{
    public int MuteId { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityValue { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
}

public class MuteMentionSourceDto
{
    public string EntityType { get; set; } = null!;
    public string EntityValue { get; set; } = null!;
}
