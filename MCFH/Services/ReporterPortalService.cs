using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class ReporterPortalService
{
    private readonly McfhDbContext _context;
    private readonly BespokeReportService _bespoke;
    private readonly ProjectAnalyticsService _analytics;

    public ReporterPortalService(
        McfhDbContext context,
        BespokeReportService bespoke,
        ProjectAnalyticsService analytics)
    {
        _context = context;
        _bespoke = bespoke;
        _analytics = analytics;
    }

    public async Task<ReporterKanbanDto?> GetKanbanAsync(int userId)
    {
        if (!await IsReporterOrAdminAsync(userId)) return null;

        var all = await _bespoke.ListPortalRequestsAsync(userId);

        return new ReporterKanbanDto
        {
            // Cần chỉnh sửa: khách mới gửi / đã giao / khách yêu cầu sửa lại
            Pending = all.Where(r => r.Status is "pending" or "assigned" or "revision_requested").ToList(),
            // Đang xử lý: đã tải file về chỉnh
            InProgress = all.Where(r => r.Status == "in_progress").ToList(),
            // Đã gửi khách: upload xong
            Completed = all.Where(r => r.Status == "completed").ToList()
        };
    }

    public async Task<PortalBespokeRequestDto?> GetRequestAsync(int userId, int requestId) =>
        await _bespoke.GetPortalRequestAsync(userId, requestId);

    public async Task<BespokeRequestItemDto?> QuoteAsync(int userId, int requestId, QuoteBespokeDto dto) =>
        await _bespoke.QuoteRequestAsync(userId, requestId, dto);

    public async Task<BespokeRequestItemDto?> StartWorkAsync(int userId, int requestId) =>
        await _bespoke.StartWorkByRequestIdAsync(userId, requestId);

    public async Task<BespokeRequestItemDto?> DeliverAsync(int userId, int requestId) =>
        await _bespoke.DeliverReportByRequestIdAsync(userId, requestId);

    public async Task<(byte[] Content, string FileName)?> DownloadAsync(int userId, int requestId) =>
        await _bespoke.DownloadByRequestIdAsync(userId, requestId);

    public async Task<BespokeRequestItemDto?> UploadRevisionAsync(
        int userId, int requestId, Stream fileStream, string fileName) =>
        await _bespoke.UploadRevisionByRequestIdAsync(userId, requestId, fileStream, fileName);

    public async Task<ReporterPerformanceDto?> GetPerformanceAsync(int userId)
    {
        if (!await IsReporterOrAdminAsync(userId)) return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var query = _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .Include(r => r.BespokeReports)
            .Where(r => IsAdmin(user) || r.ReporterId == userId);

        var list = await query.OrderByDescending(r => r.RequestId).ToListAsync();
        var completed = list.Where(r => r.Status == "completed").ToList();

        double? avgDays = null;
        var durations = completed
            .Where(r => r.AssignedAt.HasValue && r.SubmittedAt.HasValue)
            .Select(r => (r.SubmittedAt!.Value - r.AssignedAt!.Value).TotalDays)
            .ToList();
        if (durations.Count > 0) avgDays = Math.Round(durations.Average(), 1);

        var portalList = new List<PortalBespokeRequestDto>();
        foreach (var r in list)
        {
            var item = await _bespoke.GetPortalRequestAsync(userId, r.RequestId);
            if (item != null) portalList.Add(item);
        }

        return new ReporterPerformanceDto
        {
            DeliveredCount = completed.Count,
            InProgressCount = list.Count(r => r.Status == "in_progress"),
            PendingCount = list.Count(r => r.Status is "pending" or "quoted" or "assigned"),
            AvgProcessingDays = avgDays,
            History = portalList
        };
    }

    public async Task<ProjectOverviewDto?> GetRequestAnalyticsOverviewAsync(int userId, int requestId)
    {
        var req = await _bespoke.GetPortalRequestAsync(userId, requestId);
        if (req == null || req.ProjectId <= 0) return null;
        return await _analytics.GetOverviewByProjectIdAsync(req.ProjectId);
    }

    private async Task<bool> IsReporterOrAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        return IsAdmin(user) || IsReporter(user);
    }

    private static bool IsAdmin(User u) =>
        u.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsReporter(User u) =>
        u.SystemRole.Equals("Reporter", StringComparison.OrdinalIgnoreCase);
}
