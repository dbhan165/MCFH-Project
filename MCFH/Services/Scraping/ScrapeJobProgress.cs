namespace MCFH.Services.Scraping;

public class ScrapeJobProgress
{
    private readonly ScrapeJobStore _store;
    private readonly string _jobId;

    public ScrapeJobProgress(ScrapeJobStore store, string jobId)
    {
        _store = store;
        _jobId = jobId;
    }

    private ScrapeJobState? State => _store.Get(_jobId);

    public CancellationToken CancellationToken => State?.CancellationToken ?? CancellationToken.None;

    public bool IsCancellationRequested => State?.IsCancellationRequested == true;

    public void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

    public void InitPlatforms(bool facebook, bool youtube, bool tiktok, bool news = false) =>
        State?.InitPlatforms(facebook, youtube, tiktok, news);

    public void SetPhase(string phase, string? message = null) =>
        State?.SetPhase(phase, message);

    public void StartPlatform(string platform, string message) =>
        State?.SetPlatform(platform, "running", GetCount(platform), message);

    public void UpdatePlatform(string platform, int count, string message) =>
        State?.SetPlatform(platform, "running", count, message);

    public void CompletePlatform(string platform, int count, string? message = null) =>
        State?.SetPlatform(platform, "done", count, message ?? $"Hoàn tất — {count} bài");

    public void FailPlatform(string platform, string message) =>
        State?.SetPlatform(platform, "error", GetCount(platform), message);

    public void SkipPlatform(string platform, string message) =>
        State?.SetPlatform(platform, "skipped", GetCount(platform), message);

    public void SkipPendingPlatforms(string message) => State?.SkipPendingPlatforms(message);

    private int GetCount(string platform) =>
        State?.Platforms.TryGetValue(platform, out var entry) == true ? entry.Count : 0;
}
