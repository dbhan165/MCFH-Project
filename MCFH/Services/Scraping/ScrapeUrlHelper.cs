using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class ScrapeUrlHelper
{
    public static string DedupeKey(string platform, string url)
    {
        var normalized = Normalize(platform, url);
        return string.IsNullOrWhiteSpace(normalized)
            ? ""
            : $"{platform.Trim().ToLowerInvariant()}|{normalized}";
    }

    public static string Normalize(string platform, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        var clean = url.Trim().Split('#')[0];
        var queryIdx = clean.IndexOf('?');
        if (queryIdx >= 0)
            clean = clean[..queryIdx];

        clean = clean.TrimEnd('/');

        if (clean.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            clean = "https://" + clean["http://".Length..];

        return platform.ToLowerInvariant() switch
        {
            "youtube" => NormalizeYouTube(clean, url),
            "tiktok" => NormalizeTikTok(clean),
            "facebook" => NormalizeFacebook(clean),
            _ => clean.ToLowerInvariant()
        };
    }

    private static string NormalizeYouTube(string clean, string original)
    {
        var videoId = YouTubeScraper.GetVideoId(original);
        return string.IsNullOrWhiteSpace(videoId)
            ? clean.ToLowerInvariant()
            : $"youtube:video:{videoId.ToLowerInvariant()}";
    }

    private static string NormalizeTikTok(string clean) =>
        clean
            .Replace("https://m.tiktok.com", "https://www.tiktok.com", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

    private static string NormalizeFacebook(string clean)
    {
        clean = clean
            .Replace("https://m.facebook.com", "https://www.facebook.com", StringComparison.OrdinalIgnoreCase)
            .Replace("https://facebook.com", "https://www.facebook.com", StringComparison.OrdinalIgnoreCase);

        // Permalink dạng /groups/.../posts/... hoặc /permalink.php?story_fbid=...
        var permalink = Regex.Match(
            clean,
            @"(https://www\.facebook\.com/(?:groups/[^/]+/(?:posts|permalink)/\d+|[^/]+/posts/\d+|permalink\.php\?story_fbid=\d+[^#]*))",
            RegexOptions.IgnoreCase);

        if (permalink.Success)
            return permalink.Groups[1].Value.ToLowerInvariant();

        return clean.ToLowerInvariant();
    }
}
