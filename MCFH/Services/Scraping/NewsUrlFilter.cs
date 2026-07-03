namespace MCFH.Services.Scraping;

/// <summary>Lọc URL bài báo VN từ SERP (Playwright hoặc SerpApi).</summary>
public static class NewsUrlFilter
{
    public static List<string> NormalizeSites(IReadOnlyList<string>? configured)
    {
        var sites = configured?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
        return sites.Count > 0
            ? sites
            : ["vnexpress.net", "tuoitre.vn", "thanhnien.vn", "vietnamnet.vn"];
    }

    public static List<string> FilterUrls(IEnumerable<string> rawUrls, IReadOnlyList<string> sites, int max)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in rawUrls)
        {
            foreach (var candidate in ExtractCandidateUrls(raw, sites))
            {
                if (!seen.Add(candidate))
                    continue;

                result.Add(candidate);
                if (result.Count >= max)
                    return result;
            }
        }

        return result;
    }

    public static IEnumerable<string> ExtractCandidateUrls(string raw, IReadOnlyList<string> sites)
    {
        if (string.IsNullOrWhiteSpace(raw))
            yield break;

        var decoded = Uri.UnescapeDataString(raw);
        if (decoded.Contains("/url?", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(decoded, UriKind.Absolute, out var googleUri))
        {
            var q = ExtractQueryParam(googleUri.Query, "q")
                    ?? ExtractQueryParam(googleUri.Query, "url");
            if (!string.IsNullOrWhiteSpace(q))
                decoded = q;
        }

        if (!Uri.TryCreate(decoded, UriKind.Absolute, out var uri))
            yield break;

        if (uri.Scheme is not ("http" or "https"))
            yield break;

        var host = uri.Host.ToLowerInvariant();
        if (!sites.Any(s => host.Contains(s.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
            yield break;

        if (host.Contains("google.") || host.Contains("bing.") || host.Contains("microsoft."))
            yield break;

        var path = uri.AbsolutePath.ToLowerInvariant();
        if (path.Length < 8 || path is "/" or "/search")
            yield break;

        if (path.Contains("/tag/") || path.Contains("/chu-de/") || path.Contains("/topic/"))
            yield break;

        var clean = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}".TrimEnd('/');
        yield return clean;
    }

    private static string? ExtractQueryParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return null;
        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var k = Uri.UnescapeDataString(part[..idx]);
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;
            return Uri.UnescapeDataString(part[(idx + 1)..]);
        }

        return null;
    }
}
