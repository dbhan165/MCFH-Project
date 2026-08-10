using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class ThreadsTextHelper
{
    public static string NormalizePostUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (!url.StartsWith("http")) url = "https://www.threads.net" + url;

        url = Regex.Replace(url, @"/post/([^/]+)/media$", @"/post/$1", RegexOptions.IgnoreCase);

        return url;
    }

    public static string ExtractPostIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"/(?:post|reel)/([A-Za-z0-9_-]+)");
        return match.Success ? match.Groups[1].Value : Guid.NewGuid().ToString("N")[..12];
    }

    public static int? ParseCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = Regex.Match(text.Trim(), @"([\d.,]+)\s*([KkMm])?");
        if (!match.Success) return null;
        if (!double.TryParse(match.Groups[1].Value.Replace(",", "."), out var num)) return null;
        var suffix = match.Groups[2].Value.ToUpper();
        if (suffix == "K") num *= 1_000;
        else if (suffix == "M") num *= 1_000_000;
        return (int)num;
    }

    public static string NormalizeProfileUrl(string url)
    {
        url = url.Trim();
        if (!url.StartsWith("http")) url = "https://www.threads.net/" + url;
        return url;
    }
}
