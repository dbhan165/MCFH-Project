using System.Text.Json;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

internal static class TikTokApiParser
{
    private static readonly Regex VideoUrlRegex = new(
        @"https?://(?:www\.)?tiktok\.com/@[\w.\-]+/video/\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<string> ExtractVideoUrlsFromJson(string json)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            WalkForVideoUrls(doc.RootElement, urls);
        }
        catch
        {
            foreach (Match m in VideoUrlRegex.Matches(json))
                urls.Add(NormalizeUrl(m.Value));
        }

        return urls.ToList();
    }

    public static IReadOnlyList<string> ExtractCommentsFromJson(string json, int maxComments)
    {
        var comments = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            WalkForComments(doc.RootElement, comments, seen, maxComments);
        }
        catch { }

        return comments;
    }

    private static void WalkForVideoUrls(JsonElement element, HashSet<string> urls)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryBuildVideoUrl(element, out var built))
                    urls.Add(built);

                if (element.TryGetProperty("share_url", out var share) && share.ValueKind == JsonValueKind.String)
                {
                    var shareUrl = share.GetString();
                    if (!string.IsNullOrWhiteSpace(shareUrl) && VideoUrlRegex.IsMatch(shareUrl))
                        urls.Add(NormalizeUrl(shareUrl));
                }

                foreach (var prop in element.EnumerateObject())
                    WalkForVideoUrls(prop.Value, urls);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    WalkForVideoUrls(item, urls);
                break;
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    foreach (Match m in VideoUrlRegex.Matches(text))
                        urls.Add(NormalizeUrl(m.Value));
                }
                break;
        }
    }

    private static bool TryBuildVideoUrl(JsonElement obj, out string url)
    {
        url = "";
        if (!obj.TryGetProperty("id", out var idProp))
            return false;

        var id = idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString()
            : idProp.ValueKind == JsonValueKind.Number
                ? idProp.GetRawText()
                : null;

        if (string.IsNullOrWhiteSpace(id) || id.Length < 10)
            return false;

        string? uniqueId = null;
        if (obj.TryGetProperty("author", out var author) && author.ValueKind == JsonValueKind.Object)
        {
            if (author.TryGetProperty("uniqueId", out var uid))
                uniqueId = uid.GetString();
            else if (author.TryGetProperty("unique_id", out var uid2))
                uniqueId = uid2.GetString();
        }

        if (string.IsNullOrWhiteSpace(uniqueId) &&
            obj.TryGetProperty("uniqueId", out var directUid))
            uniqueId = directUid.GetString();

        if (string.IsNullOrWhiteSpace(uniqueId))
            return false;

        url = $"https://www.tiktok.com/@{uniqueId}/video/{id}";
        return true;
    }

    private static void WalkForComments(
        JsonElement element,
        List<string> comments,
        HashSet<string> seen,
        int maxComments)
    {
        if (comments.Count >= maxComments)
            return;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("text", out var textProp) &&
                    textProp.ValueKind == JsonValueKind.String)
                {
                    var isComment = element.TryGetProperty("cid", out _)
                        || element.TryGetProperty("comment_id", out _)
                        || (element.TryGetProperty("user", out var user)
                            && user.ValueKind == JsonValueKind.Object
                            && element.TryGetProperty("text", out _));

                    if (isComment)
                    {
                        var text = textProp.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 1 && seen.Add(text))
                            comments.Add(text);
                    }
                }

                foreach (var prop in element.EnumerateObject())
                    WalkForComments(prop.Value, comments, seen, maxComments);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    WalkForComments(item, comments, seen, maxComments);
                break;
        }
    }

    private static string NormalizeUrl(string url) =>
        url.Split('?')[0].Split('#')[0]
            .Replace("https://m.tiktok.com", "https://www.tiktok.com", StringComparison.OrdinalIgnoreCase);
}
