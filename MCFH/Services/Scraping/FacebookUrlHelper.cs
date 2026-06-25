namespace MCFH.Services.Scraping;

public static class FacebookUrlHelper
{
    public static string NormalizeSourceUrl(string url)
    {
        var normalized = url.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "";

        if (!normalized.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            normalized = "https://" + normalized;

        normalized = normalized
            .Replace("://m.facebook.com", "://www.facebook.com", StringComparison.OrdinalIgnoreCase)
            .Replace("://facebook.com", "://www.facebook.com", StringComparison.OrdinalIgnoreCase);

        var q = normalized.IndexOf('?');
        if (q > 0)
            normalized = normalized[..q];

        return normalized.TrimEnd('/');
    }

    /// <summary>URL tìm bài theo từ khóa trong group/page.</summary>
    public static string BuildKeywordSearchUrl(string sourceUrl, string keyword)
    {
        var baseUrl = NormalizeSourceUrl(sourceUrl);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "";

        var encoded = Uri.EscapeDataString(keyword.Trim());
        if (baseUrl.Contains("/search", StringComparison.OrdinalIgnoreCase))
        {
            return baseUrl.Contains('?')
                ? $"{baseUrl}&q={encoded}"
                : $"{baseUrl}?q={encoded}";
        }

        return $"{baseUrl}/search/?q={encoded}";
    }

    /// <summary>URL feed chính của group/page (fallback khi search không có kết quả).</summary>
    public static string BuildFeedUrl(string sourceUrl)
    {
        var baseUrl = NormalizeSourceUrl(sourceUrl);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "";

        var idx = baseUrl.IndexOf("/search", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? baseUrl[..idx].TrimEnd('/') : baseUrl;
    }

    public static bool LooksLikeFacebookUrl(string url) =>
        url.Contains("facebook.com", StringComparison.OrdinalIgnoreCase);
}
