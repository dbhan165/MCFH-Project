using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace MCFH.Controllers.Projects;

/// <summary>
/// Analytics, reports, bespoke, analyze — tách riêng để không sửa ProjectController gốc.
/// </summary>
[ApiController]
[Route("api/workspaces/{workspaceId}/projects")]
[Authorize]
public class ProjectExtendedController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly AiAnalysisService _aiAnalysisService;
    private readonly ProjectAnalyticsService _analyticsService;
    private readonly ProjectReportService _reportService;
    private readonly BespokeReportService _bespokeService;
    private readonly MentionFilterService _mentionFilters;
    private readonly MentionManagementService _mentionManagement;

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProjectExtendedController(
        McfhDbContext db,
        IProjectService projectService,
        AiAnalysisService aiAnalysisService,
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        IEmailService emailService)
    {
        _projectService = projectService;
        _aiAnalysisService = aiAnalysisService;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _analyticsService = new ProjectAnalyticsService(db);
        _mentionFilters = new MentionFilterService(db, _analyticsService);
        _mentionManagement = new MentionManagementService(db);
        _reportService = new ProjectReportService(db, _analyticsService);
        _bespokeService = new BespokeReportService(db, _analyticsService, emailService);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Hệ thống";

    [HttpPost("{projectId}/analyze")]
    public IActionResult Analyze(int workspaceId, int projectId, [FromQuery] bool force = true)
    {
        var userId = GetUserId();
        
        Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();
                await aiService.AnalyzeProjectAsync(workspaceId, projectId, userId, force);
            }
            catch (Exception ex)
            {
                // Task failed
                var cacheKey = $"Project:{projectId}:AiProgress";
                _cache.Remove(cacheKey);
            }
        });

        return Ok(new { message = "Đã đưa vào hàng đợi phân tích AI.", status = "queued" });
    }

    [HttpGet("{projectId}/analytics/progress")]
    public IActionResult GetAnalysisProgress(int workspaceId, int projectId)
    {
        var cacheKey = $"Project:{projectId}:AiProgress";
        if (_cache.TryGetValue(cacheKey, out int percent))
        {
            return Ok(new { isAnalyzing = true, progressPercent = percent });
        }
        
        return Ok(new { isAnalyzing = false, progressPercent = 0 });
    }

    [HttpGet("analytics/progress")]
    public async Task<IActionResult> GetWorkspaceAnalysisProgress(int workspaceId)
    {
        var projects = await _projectService.GetProjectsAsync(workspaceId, GetUserId());
        var result = new Dictionary<int, object>();
        foreach (var p in projects)
        {
            var cacheKey = $"Project:{p.ProjectId}:AiProgress";
            if (_cache.TryGetValue(cacheKey, out int percent))
            {
                result[p.ProjectId] = new { isAnalyzing = true, progressPercent = percent };
            }
        }
        return Ok(result);
    }

    [HttpGet("{projectId}/analytics/overview")]
    public async Task<IActionResult> GetOverview(int workspaceId, int projectId)
    {
        var result = await _analyticsService.GetOverviewAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });
        return Ok(result);
    }

    [HttpGet("{projectId}/analytics/mentions")]
    public async Task<IActionResult> GetMentions(
        int workspaceId, int projectId,
        [FromQuery] string? platform, [FromQuery] string? sentiment,
        [FromQuery] string? search, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] bool excludeMuted = true, [FromQuery] bool? isCrisisAlert = null)
    {
        var userId = GetUserId();
        var project = await _projectService.GetByIdAsync(workspaceId, projectId, userId);
        if (project == null) return NotFound(new { message = "Project không tồn tại." });

        var filter = new MentionQueryDto
        {
            Platform = platform,
            Sentiment = sentiment,
            Search = search,
            DateFrom = dateFrom,
            DateTo = dateTo,
            ExcludeMuted = excludeMuted,
            IsCrisisAlert = isCrisisAlert
        };
        return Ok(await _analyticsService.GetMentionsAsync(workspaceId, projectId, userId, filter));
    }

    [HttpGet("{projectId}/mention-tags")]
    public async Task<IActionResult> ListMentionTags(int workspaceId, int projectId) =>
        Ok(await _mentionManagement.ListTagsAsync(workspaceId, projectId, GetUserId()));

    [HttpPost("{projectId}/mention-tags")]
    public async Task<IActionResult> CreateMentionTag(int workspaceId, int projectId, [FromBody] CreateMentionTagDto dto)
    {
        var result = await _mentionManagement.CreateTagAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null) return BadRequest(new { message = "Không thể tạo tag." });
        return Ok(result);
    }

    [HttpPut("{projectId}/analytics/mentions/{feedbackId}/tags")]
    public async Task<IActionResult> AssignMentionTags(
        int workspaceId, int projectId, int feedbackId, [FromBody] AssignMentionTagsDto dto)
    {
        var ok = await _mentionManagement.AssignTagsAsync(workspaceId, projectId, GetUserId(), feedbackId, dto);
        if (!ok) return NotFound();
        return Ok(new { message = "Đã gán tag." });
    }

    [HttpPut("{projectId}/analytics/mentions/{feedbackId}/sentiment")]
    public async Task<IActionResult> UpdateMentionSentiment(
        int workspaceId, int projectId, int feedbackId, [FromBody] UpdateMentionSentimentDto dto)
    {
        var ok = await _mentionManagement.UpdateSentimentAsync(workspaceId, projectId, GetUserId(), feedbackId, dto);
        if (!ok) return BadRequest(new { message = "Không thể cập nhật sentiment." });
        return Ok(new { message = "Đã cập nhật sentiment." });
    }

    [HttpGet("{projectId}/muted-sources")]
    public async Task<IActionResult> ListMutedSources(int workspaceId, int projectId) =>
        Ok(await _mentionManagement.ListMutedAsync(workspaceId, projectId, GetUserId()));

    [HttpPost("{projectId}/muted-sources")]
    public async Task<IActionResult> MuteSource(int workspaceId, int projectId, [FromBody] MuteMentionSourceDto dto)
    {
        var result = await _mentionManagement.MuteSourceAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null) return BadRequest(new { message = "Không thể mute nguồn này." });
        return Ok(result);
    }

    [HttpDelete("{projectId}/muted-sources/{muteId}")]
    public async Task<IActionResult> UnmuteSource(int workspaceId, int projectId, int muteId)
    {
        var ok = await _mentionManagement.UnmuteAsync(workspaceId, projectId, GetUserId(), muteId);
        if (!ok) return NotFound();
        return Ok(new { message = "Đã bỏ mute." });
    }

    [HttpDelete("{projectId}/analytics/mentions/{feedbackId}")]
    public async Task<IActionResult> DeleteMention(int workspaceId, int projectId, int feedbackId)
    {
        var ok = await _analyticsService.DeleteMentionAsync(workspaceId, projectId, GetUserId(), feedbackId);
        if (!ok) return NotFound();
        return Ok(new { message = "Đã xóa mention." });
    }

    [HttpPost("{projectId}/analytics/mentions/{feedbackId}/analyze")]
    public async Task<IActionResult> AnalyzeMention(int workspaceId, int projectId, int feedbackId)
    {
        var result = await _aiAnalysisService.AnalyzeSingleFeedbackAsync(workspaceId, projectId, GetUserId(), feedbackId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{projectId}/mention-filters")]
    public async Task<IActionResult> ListMentionFilters(int workspaceId, int projectId) =>
        Ok(await _mentionFilters.ListSavedFiltersAsync(workspaceId, projectId, GetUserId()));

    [HttpPost("{projectId}/mention-filters")]
    public async Task<IActionResult> CreateMentionFilter(int workspaceId, int projectId, [FromBody] CreateSavedFilterDto dto)
    {
        var result = await _mentionFilters.CreateSavedFilterAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpPut("{projectId}/mention-filters/{filterId}")]
    public async Task<IActionResult> UpdateMentionFilter(int workspaceId, int projectId, int filterId, [FromBody] UpdateSavedFilterDto dto)
    {
        var result = await _mentionFilters.UpdateSavedFilterAsync(workspaceId, projectId, GetUserId(), filterId, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{projectId}/mention-filters/{filterId}")]
    public async Task<IActionResult> DeleteMentionFilter(int workspaceId, int projectId, int filterId)
    {
        var ok = await _mentionFilters.DeleteSavedFilterAsync(workspaceId, projectId, GetUserId(), filterId);
        if (!ok) return NotFound();
        return Ok(new { message = "Đã xóa bộ lọc." });
    }

    [HttpGet("{projectId}/analytics/sentiment")]
    public async Task<IActionResult> GetSentiment(int workspaceId, int projectId)
    {
        var result = await _analyticsService.GetSentimentSummaryAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{projectId}/analytics/influencers")]
    public async Task<IActionResult> GetInfluencers(int workspaceId, int projectId)
    {
        var result = await _analyticsService.GetInfluencersAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{projectId}/analytics/channels")]
    public async Task<IActionResult> GetChannels(int workspaceId, int projectId)
    {
        var result = await _analyticsService.GetChannelComparisonAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{projectId}/analytics/aspects")]
    public async Task<IActionResult> GetAspects(int workspaceId, int projectId)
    {
        var result = await _analyticsService.GetAspectAnalysisAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{projectId}/reports")]
    public async Task<IActionResult> GetReports(int workspaceId, int projectId)
    {
        var result = await _reportService.GetReportCenterAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{projectId}/reports/generate")]
    public async Task<IActionResult> GenerateReport(int workspaceId, int projectId, [FromBody] GenerateReportRequestDto dto)
    {
        var result = await _reportService.GenerateReportAsync(workspaceId, projectId, GetUserId(), dto.Type, GetUserName());
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpGet("{projectId}/reports/{reportId}/download")]
    public async Task<IActionResult> DownloadReport(int workspaceId, int projectId, string reportId)
    {
        var file = await _reportService.DownloadReportAsync(workspaceId, projectId, GetUserId(), reportId);
        if (file == null) return NotFound();
        return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet("{projectId}/bespoke")]
    public async Task<IActionResult> GetBespokeCenter(int workspaceId, int projectId)
    {
        var result = await _bespokeService.GetBespokeCenterAsync(workspaceId, projectId, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke")]
    public async Task<IActionResult> CreateBespokeRequest(int workspaceId, int projectId, [FromBody] CreateBespokeRequestDto dto)
    {
        var result = await _bespokeService.CreateRequestAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/assign")]
    public async Task<IActionResult> AssignBespokeReporter(int workspaceId, int projectId, int requestId, [FromBody] AssignBespokeReporterDto dto)
    {
        var result = await _bespokeService.AssignReporterAsync(workspaceId, projectId, GetUserId(), requestId, dto.ReporterId);
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/start")]
    public async Task<IActionResult> StartBespokeWork(int workspaceId, int projectId, int requestId)
    {
        var result = await _bespokeService.StartWorkAsync(workspaceId, projectId, GetUserId(), requestId);
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/deliver")]
    public async Task<IActionResult> DeliverBespokeReport(int workspaceId, int projectId, int requestId)
    {
        var result = await _bespokeService.DeliverReportAsync(workspaceId, projectId, GetUserId(), requestId);
        if (result == null) return BadRequest();
        return Ok(result);
    }

    [HttpGet("{projectId}/bespoke/{requestId}/download")]
    public async Task<IActionResult> DownloadBespokeReport(int workspaceId, int projectId, int requestId)
    {
        var file = await _bespokeService.DownloadDeliverableAsync(workspaceId, projectId, GetUserId(), requestId);
        if (file == null) return NotFound();
        var contentType = BespokeReportService.GetDeliverableContentType(file.Value.FileName);
        return File(file.Value.Content, contentType, file.Value.FileName);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/accept-quote")]
    public async Task<IActionResult> AcceptBespokeQuote(int workspaceId, int projectId, int requestId)
    {
        var result = await _bespokeService.AcceptQuoteAsync(workspaceId, projectId, GetUserId(), requestId);
        if (result == null) return BadRequest(new { message = "Không thể chấp nhận báo giá." });
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/reject-quote")]
    public async Task<IActionResult> RejectBespokeQuote(int workspaceId, int projectId, int requestId)
    {
        var result = await _bespokeService.RejectQuoteAsync(workspaceId, projectId, GetUserId(), requestId);
        if (result == null) return BadRequest(new { message = "Không thể từ chối báo giá." });
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/request-revision")]
    public async Task<IActionResult> RequestBespokeRevision(
        int workspaceId, int projectId, int requestId, [FromBody] RequestBespokeRevisionDto dto)
    {
        var result = await _bespokeService.RequestRevisionAsync(workspaceId, projectId, GetUserId(), requestId, dto);
        if (result == null) return BadRequest(new { message = "Không thể gửi yêu cầu chỉnh sửa." });
        return Ok(result);
    }

    [HttpPost("{projectId}/bespoke/{requestId}/upload-revision")]
    public async Task<IActionResult> UploadBespokeRevision(
        int workspaceId, int projectId, int requestId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file báo cáo." });

        await using var stream = file.OpenReadStream();
        var result = await _bespokeService.UploadRevisionAsync(
            workspaceId, projectId, GetUserId(), requestId, stream, file.FileName);
        if (result == null) return BadRequest(new { message = "Không thể upload bản sửa đổi." });
        return Ok(result);
    }
}
