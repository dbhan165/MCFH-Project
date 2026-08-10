using MCFH.Models.Scraping;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// Captures Threads GraphQL responses to extract post + comment data
/// WITHOUT relying on DOM selectors (which are unreliable due to React virtualization).
///
/// Threads dùng Meta's Relay GraphQL client. Khi user navigate đến post URL,
/// Threads fire 1 GraphQL POST đến /api/graphql lấy post data + replies.
/// Response JSON chứa đầy đủ: post text, username, likeCount, commentCount,
/// reply edges (mỗi reply có user.username + text).
/// </summary>
public sealed class ThreadsNetworkCapture
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ThreadsCapturedPost> _posts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, ThreadsCapturedPost> Posts
    {
        get { lock (_lock) return new Dictionary<string, ThreadsCapturedPost>(_posts); }
    }

    public void Attach(IPage page)
    {
        page.Response += async (_, response) =>
        {
            try
            {
                var url = response.Url;
                if (!url.Contains("threads.net/api/graphql", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("threads.com/api/graphql", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!response.Ok)
                    return;

                var body = await response.TextAsync();
                if (string.IsNullOrWhiteSpace(body) || body.Length < 50)
                    return;

                var captured = ThreadsApiParser.ParseGraphQLResponse(body);
                if (captured == null) return;

                lock (_lock)
                {
                    // Merge by postId; new replies overwrite old but we keep union of comments
                    if (_posts.TryGetValue(captured.PostId, out var existing))
                    {
                        // Merge replies
                        foreach (var reply in captured.Replies)
                        {
                            if (!existing.Replies.Any(r =>
                                string.Equals(r.Text, reply.Text, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(r.Username, reply.Username, StringComparison.OrdinalIgnoreCase)))
                            {
                                existing.Replies.Add(reply);
                            }
                        }
                        // Fill post metadata if missing
                        if (string.IsNullOrEmpty(existing.Username) && !string.IsNullOrEmpty(captured.Username))
                            existing.Username = captured.Username;
                        if (string.IsNullOrEmpty(existing.Text) && !string.IsNullOrEmpty(captured.Text))
                            existing.Text = captured.Text;
                        if (existing.LikeCount == 0 && captured.LikeCount > 0)
                            existing.LikeCount = captured.LikeCount;
                        if (existing.CommentCount == 0 && captured.CommentCount > 0)
                            existing.CommentCount = captured.CommentCount;
                    }
                    else
                    {
                        _posts[captured.PostId] = captured;
                    }
                }
            }
            catch
            {
                // Network parse is best-effort.
            }
        };
    }

    public bool TryGetPost(string postId, out ThreadsCapturedPost post)
    {
        lock (_lock) return _posts.TryGetValue(postId, out post!);
    }
}