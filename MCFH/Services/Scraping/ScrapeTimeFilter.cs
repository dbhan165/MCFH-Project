namespace MCFH.Services.Scraping;

/// <summary>Lọc bài theo ngày đăng — null / 0 = không giới hạn.</summary>
public sealed class ScrapeTimeFilter
{
    public int? PostedSinceDays { get; init; }

    /// <summary>Chặn trên (UTC, exclusive) — bài đăng sau mốc này bị loại. Null = không giới hạn.</summary>
    public DateTime? UntilUtc { get; init; }

    public DateTime? CutoffUtc =>
        PostedSinceDays is > 0
            ? DateTime.UtcNow.AddDays(-PostedSinceDays.Value)
            : null;

    public bool IsActive => CutoffUtc.HasValue || UntilUtc.HasValue;

    public bool IsWithinRange(DateTime? postedAt, bool allowUnknownDate = false)
    {
        if (!IsActive) return true;
        if (!postedAt.HasValue) return allowUnknownDate;

        var postedUtc = postedAt.Value.Kind switch
        {
            DateTimeKind.Utc => postedAt.Value,
            DateTimeKind.Local => postedAt.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(postedAt.Value, DateTimeKind.Local).ToUniversalTime()
        };

        if (CutoffUtc.HasValue && postedUtc < CutoffUtc.Value) return false;
        if (UntilUtc.HasValue && postedUtc >= UntilUtc.Value) return false;
        return true;
    }

    /// <summary>
    /// Toán tử after:/before: gắn vào từ khóa YouTube search khi có khoảng ngày tường minh
    /// (chỉ luồng bespoke set UntilUtc). Trả về "" nếu không có → search giữ nguyên như cũ.
    /// </summary>
    public string YouTubeSearchDateOperators()
    {
        if (!UntilUtc.HasValue) return "";

        var parts = new List<string>();
        if (CutoffUtc.HasValue)
            parts.Add($"after:{CutoffUtc.Value.ToLocalTime():yyyy-MM-dd}");
        // UntilUtc = ngày kết thúc + 1 (exclusive); before: của YouTube cũng exclusive nên khớp trọn ngày cuối.
        parts.Add($"before:{UntilUtc.Value.ToLocalTime():yyyy-MM-dd}");
        return " " + string.Join(" ", parts);
    }

    public int DiscoveryPoolSize(int targetCount) =>
        IsActive ? Math.Max(targetCount * 4, targetCount + 10) : targetCount;

    public static ScrapeTimeFilter FromDays(int? days) =>
        days is null or <= 0 ? new ScrapeTimeFilter() : new ScrapeTimeFilter { PostedSinceDays = days };

    /// <summary>Tạo filter có cả chặn dưới (số ngày) lẫn chặn trên (ngày kết thúc, local — bao trọn ngày đó).</summary>
    public static ScrapeTimeFilter FromDaysUntil(int? days, DateTime? untilLocalDate)
    {
        DateTime? untilUtc = untilLocalDate.HasValue
            ? DateTime.SpecifyKind(untilLocalDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime()
            : null;

        return new ScrapeTimeFilter
        {
            PostedSinceDays = days is > 0 ? days : null,
            UntilUtc = untilUtc
        };
    }
}
