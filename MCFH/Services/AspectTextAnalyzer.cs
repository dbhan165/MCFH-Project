namespace MCFH.Services;

public static class AspectTextAnalyzer
{
    public sealed record AspectDefinition(
        string Key,
        string Label,
        string[] Triggers,
        string[] PositiveHints,
        string[] NegativeHints);

    public static readonly AspectDefinition[] Definitions =
    [
        new("price", "Giá cả",
            ["giá", "giá cả", "chi phí", "phí", "đắt", "rẻ", "mắc", "hợp lý", "price", "cost", "expensive", "cheap"],
            ["rẻ", "giá tốt", "hợp lý", "phải chăng", "ưu đãi", "giảm giá", "cheap", "affordable"],
            ["đắt", "mắc", "giá cao", "phí cao", "quá đắt", "expensive", "overpriced"]),
        new("quality", "Chất lượng",
            ["chất lượng", "quality", "ổn định", "lỗi", "bug", "kém", "tốt", "xịn", "tệ"],
            ["chất lượng tốt", "ổn", "xịn", "ngon", "bền", "chuẩn", "excellent", "great quality"],
            ["kém", "tệ", "lỗi", "hỏng", "dở", "bug", "crash", "poor quality", "bad"]),
        new("service", "Dịch vụ / CSKH",
            ["dịch vụ", "cskh", "hỗ trợ", "chăm sóc", "support", "service", "phản hồi", "tư vấn"],
            ["nhiệt tình", "chu đáo", "hỗ trợ tốt", "phản hồi nhanh", "good service", "helpful"],
            ["chậm", "thái độ", "không hỗ trợ", "tệ", "bad service", "unresponsive"]),
        new("delivery", "Giao hàng",
            ["giao hàng", "ship", "shipping", "vận chuyển", "giao", "nhận hàng", "delay", "trễ"],
            ["giao nhanh", "ship nhanh", "đúng hẹn", "fast delivery", "on time"],
            ["giao chậm", "trễ", "delay", "chậm", "late delivery", "slow shipping"]),
        new("workplace", "Môi trường làm việc",
            ["môi trường", "văn hóa", "công ty", "làm việc", "workplace", "culture", "đồng nghiệp", "sếp", "ot", "overtime"],
            ["văn hóa tốt", "thoải mái", "work life", "đãi ngộ", "friendly", "good culture"],
            ["áp lực", "toxic", "burnout", "overtime", "căng thẳng", "stress", "bad culture"]),
        new("salary", "Lương thưởng",
            ["lương", "thưởng", "salary", "bonus", "thu nhập", "đãi ngộ", "benefit", "phúc lợi"],
            ["lương cao", "thưởng tốt", "đãi ngộ tốt", "good salary", "high pay"],
            ["lương thấp", "thưởng ít", "underpaid", "low salary", "không xứng"]),
        new("technology", "Công nghệ",
            ["công nghệ", "tech", "technology", "ai", "phần mềm", "software", "hệ thống", "platform"],
            ["hiện đại", "mới", "innovative", "tiên tiến", "smart", "advanced"],
            ["lỗi thời", "cũ", "outdated", "slow system", "lag", "buggy"])
    ];

    public sealed record AspectHit(string Key, string Label, string Sentiment);

    public static List<AspectHit> AnalyzeText(string? content, IEnumerable<string> comments)
    {
        var text = BuildCorpus(content, comments);
        if (string.IsNullOrWhiteSpace(text)) return new List<AspectHit>();

        var lower = text.ToLowerInvariant();
        var hits = new List<AspectHit>();

        foreach (var def in Definitions)
        {
            if (!def.Triggers.Any(t => lower.Contains(t, StringComparison.OrdinalIgnoreCase)))
                continue;

            var sentiment = DetectAspectSentiment(lower, def);
            hits.Add(new AspectHit(def.Key, def.Label, sentiment));
        }

        return hits;
    }

    private static string BuildCorpus(string? content, IEnumerable<string> comments)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(content)) parts.Add(content);
        parts.AddRange(comments.Where(c => !string.IsNullOrWhiteSpace(c)));
        return string.Join("\n", parts);
    }

    private static string DetectAspectSentiment(string lowerText, AspectDefinition def)
    {
        var pos = CountHints(lowerText, def.PositiveHints);
        var neg = CountHints(lowerText, def.NegativeHints);

        if (pos > neg) return "positive";
        if (neg > pos) return "negative";
        return "neutral";
    }

    private static int CountHints(string text, string[] hints) =>
        hints.Count(h => text.Contains(h, StringComparison.OrdinalIgnoreCase));
}
