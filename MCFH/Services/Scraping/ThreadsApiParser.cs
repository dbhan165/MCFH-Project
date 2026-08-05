using System.Text.Json;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

/// <summary>
/// Parses Threads GraphQL response JSON to extract post + reply data.
///
/// Threads response có cấu trúc:
/// {
///   "data": {
///     "data": {  // nested: outer GraphQL envelope → inner data key
///       "post": {
///         "id": "...",
///         "code": "DaMw5dIDcpn",  // post slug (dùng làm PostId)
///         "user": { "username": "..." },
///         "caption": { "text": "..." },
///         "like_count": 13600,
///         "text_post_app_info": { "direct_reply_count": 138 },
///         "thread_items": [   // replies
///           {
///             "id": "...",
///             "post": {       // nested post representing the reply
///               "id": "...",
///               "user": { "username": "..." },
///               "caption": { "text": "..." },
///               "like_count": 2
///             }
///           }
///         ]
///       }
///     }
///   }
/// }
///
/// </summary>
internal static class ThreadsApiParser
{
    public static ThreadsCapturedPost? ParseGraphQLResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return WalkForPost(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static ThreadsCapturedPost? WalkForPost(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryExtractPost(element, out var captured))
                    return captured;
                // Recurse into all properties
                foreach (var prop in element.EnumerateObject())
                {
                    var result = WalkForPost(prop.Value);
                    if (result != null) return result;
                }
                return null;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var result = WalkForPost(item);
                    if (result != null) return result;
                }
                return null;
        }
        return null;
    }

    private static bool TryExtractPost(JsonElement obj, out ThreadsCapturedPost post)
    {
        post = new ThreadsCapturedPost();

        // Check this object looks like a post: must have user.username AND caption.text OR like_count
        if (!obj.TryGetProperty("user", out var userEl) || userEl.ValueKind != JsonValueKind.Object)
            return false;

        // Get username
        var username = TryGetUsername(userEl);
        if (string.IsNullOrEmpty(username))
            return false;

        // Get text from caption.text
        string text = "";
        if (obj.TryGetProperty("caption", out var captionEl) && captionEl.ValueKind == JsonValueKind.Object)
        {
            if (captionEl.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                text = textEl.GetString() ?? "";
        }

        // Get post id (try code first as it's stable, fallback to id)
        string postId = "";
        if (obj.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
            postId = codeEl.GetString() ?? "";
        if (string.IsNullOrEmpty(postId) && obj.TryGetProperty("id", out var idEl))
            postId = idEl.GetString() ?? idEl.GetRawText();

        if (string.IsNullOrEmpty(postId))
            return false;

        post.PostId = postId;
        post.Username = username;
        post.Text = text;

        // Like count
        if (obj.TryGetProperty("like_count", out var likeEl) && likeEl.ValueKind == JsonValueKind.Number)
            post.LikeCount = likeEl.GetInt64();

        // Comment count: try text_post_app_info.direct_reply_count
        if (obj.TryGetProperty("text_post_app_info", out var appInfo) && appInfo.ValueKind == JsonValueKind.Object)
        {
            if (appInfo.TryGetProperty("direct_reply_count", out var replyCountEl) && replyCountEl.ValueKind == JsonValueKind.Number)
                post.CommentCount = replyCountEl.GetInt64();
            else if (appInfo.TryGetProperty("reply_count", out var rc2) && rc2.ValueKind == JsonValueKind.Number)
                post.CommentCount = rc2.GetInt64();
        }
        // Fallback: thread_items length is the number of loaded replies
        if (post.CommentCount == 0 &&
            obj.TryGetProperty("thread_items", out var threadItems) &&
            threadItems.ValueKind == JsonValueKind.Array)
        {
            // Use as estimate
            post.CommentCount = threadItems.GetArrayLength();
        }

        // Extract replies
        if (obj.TryGetProperty("thread_items", out var threadItems2) && threadItems2.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in threadItems2.EnumerateArray())
            {
                var reply = TryExtractReply(item);
                if (reply != null)
                    post.Replies.Add(reply);
            }
        }

        return true;
    }

    private static ThreadsCapturedReply? TryExtractReply(JsonElement item)
    {
        // Each thread_item has structure { id, post: {...} } OR is the post itself directly
        JsonElement replyObj = item;
        if (item.TryGetProperty("post", out var nestedPost) && nestedPost.ValueKind == JsonValueKind.Object)
        {
            replyObj = nestedPost;
        }

        // Must have user.username
        if (!replyObj.TryGetProperty("user", out var userEl) || userEl.ValueKind != JsonValueKind.Object)
            return null;

        var username = TryGetUsername(userEl);
        if (string.IsNullOrEmpty(username))
            return null;

        var text = "";
        if (replyObj.TryGetProperty("caption", out var captionEl) && captionEl.ValueKind == JsonValueKind.Object)
        {
            if (captionEl.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                text = textEl.GetString() ?? "";
        }

        // Skip empty text
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var reply = new ThreadsCapturedReply
        {
            Username = username,
            Text = text
        };

        if (replyObj.TryGetProperty("like_count", out var likeEl) && likeEl.ValueKind == JsonValueKind.Number)
            reply.LikeCount = likeEl.GetInt64();

        if (replyObj.TryGetProperty("id", out var idEl))
            reply.ReplyId = idEl.GetString() ?? idEl.GetRawText();

        return reply;
    }

    private static string TryGetUsername(JsonElement userEl)
    {
        if (userEl.TryGetProperty("username", out var u) && u.ValueKind == JsonValueKind.String)
            return u.GetString() ?? "";
        return "";
    }
}