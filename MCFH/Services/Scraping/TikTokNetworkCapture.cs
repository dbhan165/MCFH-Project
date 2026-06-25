using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

internal sealed class TikTokNetworkCapture
{
    private readonly object _lock = new();
    private readonly HashSet<string> _videoUrls = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _comments = new();
    private readonly HashSet<string> _commentSeen = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> VideoUrls
    {
        get { lock (_lock) return _videoUrls.ToList(); }
    }

    public IReadOnlyList<string> Comments
    {
        get { lock (_lock) return _comments.ToList(); }
    }

    public void Attach(IPage page, int maxComments = 50)
    {
        page.Response += async (_, response) =>
        {
            try
            {
                var url = response.Url;
                if (!url.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
                    return;

                var isSearch = url.Contains("/api/search/", StringComparison.OrdinalIgnoreCase);
                var isComment = url.Contains("/api/comment/", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("/comment/list", StringComparison.OrdinalIgnoreCase);

                if (!isSearch && !isComment)
                    return;

                if (!response.Ok)
                    return;

                var body = await response.TextAsync();
                if (string.IsNullOrWhiteSpace(body) || body.Length < 20)
                    return;

                if (isSearch)
                {
                    foreach (var videoUrl in TikTokApiParser.ExtractVideoUrlsFromJson(body))
                        AddVideoUrl(videoUrl);
                }

                if (isComment)
                {
                    foreach (var text in TikTokApiParser.ExtractCommentsFromJson(body, maxComments))
                        AddComment(text);
                }
            }
            catch
            {
                // Network parse is best-effort.
            }
        };
    }

    private void AddVideoUrl(string url)
    {
        lock (_lock)
            _videoUrls.Add(url);
    }

    private void AddComment(string text)
    {
        lock (_lock)
        {
            if (_commentSeen.Add(text))
                _comments.Add(text);
        }
    }
}
