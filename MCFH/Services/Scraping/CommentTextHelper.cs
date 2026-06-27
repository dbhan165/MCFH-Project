using System.Text.RegularExpressions;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public static class CommentTextHelper
{
    private static readonly Regex NoisePattern = new(
        @"^(Like|Thích|Reply|Phản hồi|Share|Chia sẻ|Follow|Theo dõi|Send|Gửi|\d+\s*(phút|giờ|ngày|h|m|d|w|y)|\d+$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FacebookMetaLabel = new(
        @"^(Bình luận|Comment)\s+(của|by|dưới tên|from)\b|\b\d+\s*(phút|giờ|ngày|tuần|tháng|năm|h|m|d|w|y)\s+trước$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VietnameseSentenceHint = new(
        @"\b(là|có|không|sao|hả|ạ|nhé|được|nên|sẽ|rất|quá|và|nhưng|thì|mà|để|cho|với|trong|ngoài|người|thôi|nhỉ|ơi|gì|đâu|vậy|đi|đó|này|kia|mình|tôi|bạn|anh|chị|em|ông|bà|con|các|những|đã|đang|vẫn|còn|chỉ|lại|cũng|hay|hoặc|nếu|khi|vì|nên|muốn|biết|hiểu|nghĩ|thấy|bảo|nói|hỏi|trả lời)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<string> Normalize(IEnumerable<string>? texts, int max = 100)
    {
        if (texts == null) return new List<string>();

        return texts
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(CollapseWhitespace)
            .Where(t => t.Length >= 2 && t.Length <= 2000)
            .Where(t => !NoisePattern.IsMatch(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    public static List<string> FilterFacebook(IEnumerable<string>? texts, int max = 100)
    {
        var normalized = Normalize(texts, max * 3);

        var filtered = normalized
            .Where(t => !FacebookMetaLabel.IsMatch(t))
            .Where(t => !LooksLikeNameTagOnly(t))
            .ToList();

        filtered = RemoveSubsumedFragments(filtered);

        return filtered
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    private static readonly Regex TikTokMetaLabel = new(
        @"\btrên TikTok\b|\bon TikTok\b|^Log in to comment|^Đăng nhập để bình luận|^See translation$|^Xem bản dịch$|^View \d+ repl|^Xem \d+ phản hồi|^\d{1,2}-\d{1,2}$|^\d{4}-\d{1,2}-\d{1,2}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubeMetaLabel = new(
        @"^(Reply|Phản hồi|\d+ replies?|\d+ phản hồi|Pinned by|Được ghim bởi|Show more replies|Hiển thị thêm phản hồi|Read more|Đọc thêm)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<string> FilterTikTok(
        IEnumerable<string>? texts,
        int max = 100,
        string? videoTitle = null,
        string? author = null)
    {
        var normalized = Normalize(texts, max * 3);
        var titleKey = NormalizeKey(videoTitle);
        var authorKey = NormalizeKey(author);

        return normalized
            .Where(t => !TikTokMetaLabel.IsMatch(t))
            .Where(t => !MatchesContextKey(t, titleKey, authorKey))
            .Where(t => !LooksLikeUiChrome(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    public static List<string> FilterYouTube(
        IEnumerable<string>? texts,
        int max = 100,
        string? videoTitle = null,
        string? channel = null)
    {
        var normalized = Normalize(texts, max * 3);
        var titleKey = NormalizeKey(videoTitle);
        var channelKey = NormalizeKey(channel);

        return normalized
            .Where(t => !YouTubeMetaLabel.IsMatch(t))
            .Where(t => !MatchesContextKey(t, titleKey, channelKey))
            .Where(t => !LooksLikeUiChrome(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    public static List<string> FromScraped(
        IEnumerable<ScrapedComment> comments,
        int max = 100,
        string? platform = null,
        string? contextTitle = null,
        string? contextAuthor = null) =>
        platform?.ToLowerInvariant() switch
        {
            "tiktok" => FilterTikTok(comments.Select(c => c.Text), max, contextTitle, contextAuthor),
            "youtube" => FilterYouTube(comments.Select(c => c.Text), max, contextTitle, contextAuthor),
            "facebook" => FilterFacebook(comments.Select(c => c.Text), max),
            _ => Normalize(comments.Select(c => c.Text), max)
        };

    private static string NormalizeKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return CollapseWhitespace(text).ToLowerInvariant();
    }

    private static bool MatchesContextKey(string text, params string[] keys)
    {
        var normalized = NormalizeKey(text);
        if (string.IsNullOrWhiteSpace(normalized)) return true;

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (normalized == key) return true;
            if (key.Length >= 20 && normalized.Contains(key, StringComparison.Ordinal)) return true;
            if (normalized.Length >= 20 && key.Contains(normalized, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static bool LooksLikeUiChrome(string text)
    {
        if (text.Length <= 3) return true;
        if (text.Equals("Reply", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("Phản hồi", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool LooksLikeNameTagOnly(string text)
    {
        if (text.Length > 45) return false;
        if (text.Contains('@')) return true;
        if (text.Contains('.', StringComparison.Ordinal) || text.Contains('?', StringComparison.Ordinal)
            || text.Contains('!', StringComparison.Ordinal) || text.Contains(',', StringComparison.Ordinal))
            return false;

        if (VietnameseSentenceHint.IsMatch(text)) return false;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is < 1 or > 4) return false;

        return words.All(w => char.IsUpper(w[0]) || char.IsDigit(w[0]));
    }

    private static List<string> RemoveSubsumedFragments(List<string> texts)
    {
        return texts
            .Where(current => !texts.Any(other =>
                !string.Equals(other, current, StringComparison.OrdinalIgnoreCase)
                && other.Contains(current, StringComparison.OrdinalIgnoreCase)
                && other.Length > current.Length + 4))
            .ToList();
    }

    private static string CollapseWhitespace(string text) =>
        Regex.Replace(text.Trim(), @"\s+", " ");
}
