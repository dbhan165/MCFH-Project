using System.Collections.Concurrent;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public class ScrapeJobStore
{
    private readonly ConcurrentDictionary<string, ScrapeJobState> _jobs = new();
    private static readonly TimeSpan JobTtl = TimeSpan.FromHours(2);

    public ScrapeJobState Create(string jobId, int projectId, int userId, int? postedSinceDays = null, bool fastDemo = false)
    {
        CleanupExpired();
        var state = new ScrapeJobState(jobId, projectId, userId, postedSinceDays, fastDemo);
        _jobs[jobId] = state;
        return state;
    }

    public ScrapeJobState? Get(string jobId) =>
        _jobs.TryGetValue(jobId, out var state) ? state : null;

    public bool RequestCancel(string jobId, int userId)
    {
        var job = Get(jobId);
        if (job == null || job.UserId != userId || job.Status != "running")
            return false;

        job.RequestCancel();
        return true;
    }

    private void CleanupExpired()
    {
        var cutoff = DateTime.UtcNow - JobTtl;
        foreach (var pair in _jobs)
        {
            if (pair.Value.CreatedAtUtc < cutoff)
                _jobs.TryRemove(pair.Key, out _);
        }
    }
}

public class ScrapeJobState
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cts = new();

    public ScrapeJobState(string jobId, int projectId, int userId, int? postedSinceDays = null, bool fastDemo = false)
    {
        JobId = jobId;
        ProjectId = projectId;
        UserId = userId;
        PostedSinceDays = postedSinceDays;
        FastDemo = fastDemo;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string JobId { get; }
    public int ProjectId { get; }
    public int UserId { get; }
    public int? PostedSinceDays { get; }
    public bool FastDemo { get; }
    public DateTime CreatedAtUtc { get; }
    public string Status { get; private set; } = "running";
    public string? Phase { get; private set; }
    public string? PhaseMessage { get; private set; }
    public ScrapeByKeywordResult? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, ScrapePlatformProgressDto> Platforms { get; } = new(StringComparer.OrdinalIgnoreCase);

    public CancellationToken CancellationToken => _cts.Token;
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void RequestCancel()
    {
        lock (_lock)
        {
            if (Status != "running") return;
            Phase = "cancelling";
            PhaseMessage = "Đang dừng — giữ dữ liệu đã cào...";
        }
        _cts.Cancel();
    }

    public void InitPlatforms(bool facebook, bool youtube, bool tiktok, bool news = false)
    {
        lock (_lock)
        {
            SetPlatformLocked("facebook", "Facebook", facebook ? "pending" : "skipped",
                facebook ? "Chờ cào..." : "Chưa bật nguồn Facebook");
            SetPlatformLocked("youtube", "YouTube", youtube ? "pending" : "skipped",
                youtube ? "Chờ cào..." : "Chưa bật nguồn YouTube");
            SetPlatformLocked("tiktok", "TikTok", tiktok ? "pending" : "skipped",
                tiktok ? "Chờ cào..." : "Chưa bật nguồn TikTok");
            SetPlatformLocked("news", "Tin tức", news ? "pending" : "skipped",
                news ? "Chờ cào..." : "Chưa bật nguồn tin tức");
        }
    }

    public void SetPhase(string phase, string? message = null)
    {
        lock (_lock)
        {
            Phase = phase;
            PhaseMessage = message;
        }
    }

    public void SetPlatform(string platform, string status, int count, string? message)
    {
        lock (_lock)
        {
            if (!Platforms.TryGetValue(platform, out var entry))
            {
                entry = new ScrapePlatformProgressDto { Platform = platform, Label = platform };
                Platforms[platform] = entry;
            }

            entry.Status = status;
            entry.Count = count;
            if (!string.IsNullOrWhiteSpace(message))
                entry.Message = message;
        }
    }

    public void SkipPendingPlatforms(string message)
    {
        lock (_lock)
        {
            foreach (var entry in Platforms.Values.ToList())
            {
                if (entry.Status is "pending" or "running")
                    SetPlatform(entry.Platform, "skipped", entry.Count, message);
            }
        }
    }

    public void Complete(ScrapeByKeywordResult result)
    {
        lock (_lock)
        {
            Status = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "completed" : "failed";
            Result = result;
            ErrorMessage = result.ErrorMessage;
            Phase = "completed";
            PhaseMessage = result.Message;
        }
    }

    public void CompleteCancelled(ScrapeByKeywordResult result)
    {
        lock (_lock)
        {
            Status = "cancelled";
            Result = result;
            ErrorMessage = null;
            Phase = "cancelled";
            PhaseMessage = result.Message;
        }
    }

    public void Fail(string message)
    {
        lock (_lock)
        {
            Status = "failed";
            ErrorMessage = message;
            Phase = "failed";
            PhaseMessage = message;
        }
    }

    public ScrapeJobStatusDto ToDto()
    {
        lock (_lock)
        {
            return new ScrapeJobStatusDto
            {
                JobId = JobId,
                ProjectId = ProjectId,
                Status = Status,
                Phase = Phase,
                PhaseMessage = PhaseMessage,
                Platforms = Platforms.Values
                    .OrderBy(p => PlatformOrder(p.Platform))
                    .ToList(),
                Result = Result,
                ErrorMessage = ErrorMessage
            };
        }
    }

    private static int PlatformOrder(string platform) => platform.ToLowerInvariant() switch
    {
        "facebook" => 0,
        "youtube" => 1,
        "tiktok" => 2,
        _ => 9
    };

    private void SetPlatformLocked(string platform, string label, string status, string? message)
    {
        Platforms[platform] = new ScrapePlatformProgressDto
        {
            Platform = platform,
            Label = label,
            Status = status,
            Count = 0,
            Message = message
        };
    }
}
