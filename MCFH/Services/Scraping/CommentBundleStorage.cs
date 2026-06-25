using System.Text.Json;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public static class CommentBundleStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetCommentsFolder() =>
        Path.Combine(AppContext.BaseDirectory, "StorageData", "comments");

    public static string GetBundlePath(int feedbackId) =>
        Path.Combine(GetCommentsFolder(), $"{feedbackId}.json");

    public static string GetRelativeBundlePath(int feedbackId) =>
        Path.Combine("StorageData", "comments", $"{feedbackId}.json");

    public static string ResolveStoredPath(int feedbackId, string? storedPath)
    {
        var canonical = GetBundlePath(feedbackId);
        if (File.Exists(canonical)) return canonical;

        if (!string.IsNullOrWhiteSpace(storedPath))
        {
            if (File.Exists(storedPath)) return storedPath;

            if (!Path.IsPathRooted(storedPath))
            {
                var relative = Path.Combine(AppContext.BaseDirectory, storedPath);
                if (File.Exists(relative)) return relative;
            }
        }

        return canonical;
    }

    public static async Task<int> SaveAsync(int feedbackId, IEnumerable<string> comments)
    {
        var existing = await LoadAsync(feedbackId);
        var normalized = CommentTextHelper.Normalize(comments);

        var bundle = new StoredCommentBundle
        {
            Comments = normalized,
            AiSummary = existing.AiSummary,
            AnalyzedAt = existing.AnalyzedAt
        };

        Directory.CreateDirectory(GetCommentsFolder());
        await File.WriteAllTextAsync(GetBundlePath(feedbackId), JsonSerializer.Serialize(bundle, JsonOptions));
        return bundle.Comments.Count;
    }

    public static async Task<StoredCommentBundle> LoadAsync(int feedbackId, string? filePath = null)
    {
        var paths = new List<string> { GetBundlePath(feedbackId) };

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            paths.Insert(0, filePath);
            if (!Path.IsPathRooted(filePath))
                paths.Add(Path.Combine(AppContext.BaseDirectory, filePath));
        }

        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var json = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                if (json.TrimStart().StartsWith('['))
                {
                    var legacy = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    return new StoredCommentBundle { Comments = legacy };
                }

                var bundle = JsonSerializer.Deserialize<StoredCommentBundle>(json, JsonOptions);
                if (bundle != null)
                    return bundle;
            }
            catch { }
        }

        return new StoredCommentBundle();
    }

    public static async Task SaveAiSummaryAsync(int feedbackId, string? summary, string? filePath = null)
    {
        // Luôn merge từ file chuẩn — tránh ghi đè mất comment khi path DB lệch
        var bundle = await LoadAsync(feedbackId, filePath);
        if (bundle.Comments.Count == 0)
        {
            var canonical = await LoadAsync(feedbackId);
            if (canonical.Comments.Count > 0)
                bundle.Comments = canonical.Comments;
        }

        bundle.AiSummary = summary;
        bundle.AnalyzedAt = DateTime.Now;

        var canonicalPath = GetBundlePath(feedbackId);
        Directory.CreateDirectory(GetCommentsFolder());
        await File.WriteAllTextAsync(canonicalPath, JsonSerializer.Serialize(bundle, JsonOptions));
    }

    public static bool BundleFileExists(int feedbackId) =>
        File.Exists(GetBundlePath(feedbackId));

    public static string BuildCombinedAnalysisText(string? caption, IReadOnlyList<string> comments)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(caption))
            parts.Add($"[Nội dung bài]\n{caption.Trim()}");

        if (comments.Count > 0)
        {
            var block = string.Join("\n", comments.Select((c, i) => $"{i + 1}. {c}"));
            parts.Add($"[Bình luận — {comments.Count} câu]\n{block}");
        }

        return string.Join("\n\n", parts);
    }
}
