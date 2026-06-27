namespace MCFH.Services.Scraping;

/// <summary>Lọc bài theo ngày đăng — null / 0 = không giới hạn.</summary>
public sealed class ScrapeTimeFilter
{
    public int? PostedSinceDays { get; init; }

    public DateTime? CutoffUtc =>
        PostedSinceDays is > 0
            ? DateTime.UtcNow.AddDays(-PostedSinceDays.Value)
            : null;

    public bool IsActive => CutoffUtc.HasValue;

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

        return postedUtc >= CutoffUtc!.Value;
    }

    public int DiscoveryPoolSize(int targetCount) =>
        IsActive ? Math.Max(targetCount * 4, targetCount + 10) : targetCount;

    public static ScrapeTimeFilter FromDays(int? days) =>
        days is null or <= 0 ? new ScrapeTimeFilter() : new ScrapeTimeFilter { PostedSinceDays = days };
}
