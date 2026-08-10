namespace MCFH.Models.Scraping;

public class ThreadsScrapeResult
{
    public bool Success { get; set; }
    public string? ProfileUrl { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? FollowerCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ThreadsPost> Posts { get; set; } = [];
}

public class ThreadsPost
{
    public string PostId { get; set; } = "";
    public string? PostUrl { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorDisplayName { get; set; }
    public string? Text { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? LikeCount { get; set; }
    public int? CommentCount { get; set; }
    public int? ViewCount { get; set; }
    public List<string> Comments { get; set; } = [];
}

/// <summary>
/// Represents post data captured from Threads GraphQL network responses.
/// Used by ThreadsNetworkCapture to store parsed post/reply data without relying on DOM selectors.
/// </summary>
public sealed class ThreadsCapturedPost
{
    public string PostId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public List<ThreadsCapturedReply> Replies { get; set; } = [];
}

/// <summary>
/// Represents a reply/comment captured from Threads GraphQL responses.
/// </summary>
public sealed class ThreadsCapturedReply
{
    public string ReplyId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public long LikeCount { get; set; }
}
