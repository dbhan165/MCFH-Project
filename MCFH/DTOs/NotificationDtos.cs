namespace MCFH.DTOs;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public string? Type { get; set; }
    public string? RelatedType { get; set; }
    public int? RelatedId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
