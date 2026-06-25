using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Projects
{
    [ApiController]
    [Route("api/workspaces/{workspaceId}/projects")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly AiAnalysisService _aiAnalysisService;
        private readonly ProjectAnalyticsService _analyticsService;
        private readonly ProjectReportService _reportService;
        private readonly BespokeReportService _bespokeService;
        private readonly MentionFilterService _mentionFilters;

        public ProjectController(
            IProjectService projectService,
            AiAnalysisService aiAnalysisService,
            ProjectAnalyticsService analyticsService,
            ProjectReportService reportService,
            BespokeReportService bespokeService,
            MentionFilterService mentionFilters)
        {
            _projectService = projectService;
            _aiAnalysisService = aiAnalysisService;
            _analyticsService = analyticsService;
            _reportService = reportService;
            _bespokeService = bespokeService;
            _mentionFilters = mentionFilters;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim!);
        }

        private string GetUserName() =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? "Hệ thống";

        // GET api/workspaces/1/projects
        [HttpGet]
        public async Task<IActionResult> GetProjects(int workspaceId)
        {
            var userId = GetUserId();
            var result = await _projectService.GetProjectsAsync(workspaceId, userId);
            return Ok(result);
        }

        // GET api/workspaces/1/projects/5
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetById(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _projectService.GetByIdAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // POST api/workspaces/1/projects
        [HttpPost]
        public async Task<IActionResult> Create(int workspaceId, [FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _projectService.CreateAsync(workspaceId, userId, dto);

            if (result == null)
                return BadRequest(new { message = "Không thể tạo project. Bạn cần quyền Owner hoặc Editor." });

            return CreatedAtAction(nameof(GetById),
                new { workspaceId, projectId = result.ProjectId }, result);
        }

        // PUT api/workspaces/1/projects/5
        [HttpPut("{projectId}")]
        public async Task<IActionResult> Update(int workspaceId, int projectId, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _projectService.UpdateAsync(workspaceId, projectId, userId, dto);

            if (result == null)
                return BadRequest(new { message = "Không thể cập nhật project. Bạn cần quyền Owner hoặc Editor, hoặc project không tồn tại." });

            return Ok(result);
        }

        // DELETE api/workspaces/1/projects/5
        [HttpDelete("{projectId}")]
        public async Task<IActionResult> Delete(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var success = await _projectService.DeleteAsync(workspaceId, projectId, userId);

            if (!success)
                return BadRequest(new { message = "Không thể xóa project. Bạn cần quyền Owner hoặc Editor, hoặc project không tồn tại." });

            return Ok(new { message = "Xóa project thành công" });
        }

        // POST api/workspaces/1/projects/5/analyze?force=true (mặc định true)
        [HttpPost("{projectId}/analyze")]
        public async Task<IActionResult> Analyze(int workspaceId, int projectId, [FromQuery] bool force = true)
        {
            var userId = GetUserId();
            var result = await _aiAnalysisService.AnalyzeProjectAsync(workspaceId, projectId, userId, force);

            if (result == null)
                return BadRequest(new { message = "Không thể phân tích project. Kiểm tra quyền truy cập hoặc project không tồn tại." });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/analytics/overview
        [HttpGet("{projectId}/analytics/overview")]
        public async Task<IActionResult> GetOverview(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _analyticsService.GetOverviewAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/analytics/mentions
        [HttpGet("{projectId}/analytics/mentions")]
        public async Task<IActionResult> GetMentions(
            int workspaceId,
            int projectId,
            [FromQuery] string? platform,
            [FromQuery] string? sentiment,
            [FromQuery] string? search,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var userId = GetUserId();
            var project = await _projectService.GetByIdAsync(workspaceId, projectId, userId);
            if (project == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            var filter = new MentionQueryDto
            {
                Platform = platform,
                Sentiment = sentiment,
                Search = search,
                DateFrom = dateFrom,
                DateTo = dateTo
            };
            var result = await _analyticsService.GetMentionsAsync(workspaceId, projectId, userId, filter);
            return Ok(result);
        }

        [HttpDelete("{projectId}/analytics/mentions/{feedbackId}")]
        public async Task<IActionResult> DeleteMention(int workspaceId, int projectId, int feedbackId)
        {
            var userId = GetUserId();
            var ok = await _analyticsService.DeleteMentionAsync(workspaceId, projectId, userId, feedbackId);
            if (!ok)
                return NotFound(new { message = "Mention không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(new { message = "Đã xóa mention." });
        }

        [HttpPost("{projectId}/analytics/mentions/{feedbackId}/analyze")]
        public async Task<IActionResult> AnalyzeMention(int workspaceId, int projectId, int feedbackId)
        {
            var userId = GetUserId();
            var result = await _aiAnalysisService.AnalyzeSingleFeedbackAsync(
                workspaceId, projectId, userId, feedbackId);

            if (result == null)
                return NotFound(new { message = "Mention không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        [HttpGet("{projectId}/mention-filters")]
        public async Task<IActionResult> ListMentionFilters(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _mentionFilters.ListSavedFiltersAsync(workspaceId, projectId, userId);
            return Ok(result);
        }

        [HttpPost("{projectId}/mention-filters")]
        public async Task<IActionResult> CreateMentionFilter(
            int workspaceId, int projectId, [FromBody] CreateSavedFilterDto dto)
        {
            var userId = GetUserId();
            var result = await _mentionFilters.CreateSavedFilterAsync(workspaceId, projectId, userId, dto);
            if (result == null) return BadRequest(new { message = "Không thể lưu bộ lọc." });
            return Ok(result);
        }

        [HttpPut("{projectId}/mention-filters/{filterId}")]
        public async Task<IActionResult> UpdateMentionFilter(
            int workspaceId, int projectId, int filterId, [FromBody] UpdateSavedFilterDto dto)
        {
            var userId = GetUserId();
            var result = await _mentionFilters.UpdateSavedFilterAsync(workspaceId, projectId, userId, filterId, dto);
            if (result == null) return NotFound(new { message = "Không tìm thấy bộ lọc." });
            return Ok(result);
        }

        [HttpDelete("{projectId}/mention-filters/{filterId}")]
        public async Task<IActionResult> DeleteMentionFilter(int workspaceId, int projectId, int filterId)
        {
            var userId = GetUserId();
            var ok = await _mentionFilters.DeleteSavedFilterAsync(workspaceId, projectId, userId, filterId);
            if (!ok) return NotFound(new { message = "Không tìm thấy bộ lọc." });
            return Ok(new { message = "Đã xóa bộ lọc." });
        }

        // GET api/workspaces/1/projects/5/analytics/sentiment
        [HttpGet("{projectId}/analytics/sentiment")]
        public async Task<IActionResult> GetSentiment(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _analyticsService.GetSentimentSummaryAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/analytics/influencers
        [HttpGet("{projectId}/analytics/influencers")]
        public async Task<IActionResult> GetInfluencers(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _analyticsService.GetInfluencersAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/analytics/channels
        [HttpGet("{projectId}/analytics/channels")]
        public async Task<IActionResult> GetChannels(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _analyticsService.GetChannelComparisonAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/analytics/aspects
        [HttpGet("{projectId}/analytics/aspects")]
        public async Task<IActionResult> GetAspects(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _analyticsService.GetAspectAnalysisAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/reports
        [HttpGet("{projectId}/reports")]
        public async Task<IActionResult> GetReports(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _reportService.GetReportCenterAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // POST api/workspaces/1/projects/5/reports/generate
        [HttpPost("{projectId}/reports/generate")]
        public async Task<IActionResult> GenerateReport(
            int workspaceId, int projectId, [FromBody] GenerateReportRequestDto dto)
        {
            var userId = GetUserId();
            var result = await _reportService.GenerateReportAsync(
                workspaceId, projectId, userId, dto.Type, GetUserName());

            if (result == null)
                return BadRequest(new { message = "Không thể tạo báo cáo. Kiểm tra loại báo cáo hoặc quyền truy cập." });

            return Ok(result);
        }

        // GET api/workspaces/1/projects/5/reports/REP-xxx/download
        [HttpGet("{projectId}/reports/{reportId}/download")]
        public async Task<IActionResult> DownloadReport(int workspaceId, int projectId, string reportId)
        {
            var userId = GetUserId();
            var file = await _reportService.DownloadReportAsync(workspaceId, projectId, userId, reportId);

            if (file == null)
                return NotFound(new { message = "Không tìm thấy báo cáo." });

            return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }

        // ——— Báo cáo chuyên sâu (Bespoke) ———

        [HttpGet("{projectId}/bespoke")]
        public async Task<IActionResult> GetBespokeCenter(int workspaceId, int projectId)
        {
            var result = await _bespokeService.GetBespokeCenterAsync(workspaceId, projectId, GetUserId());
            if (result == null) return NotFound(new { message = "Không có quyền truy cập." });
            return Ok(result);
        }

        [HttpPost("{projectId}/bespoke")]
        public async Task<IActionResult> CreateBespokeRequest(
            int workspaceId, int projectId, [FromBody] CreateBespokeRequestDto dto)
        {
            var result = await _bespokeService.CreateRequestAsync(workspaceId, projectId, GetUserId(), dto);
            if (result == null) return BadRequest(new { message = "Không thể tạo yêu cầu báo cáo." });
            return Ok(result);
        }

        [HttpPost("{projectId}/bespoke/{requestId}/assign")]
        public async Task<IActionResult> AssignBespokeReporter(
            int workspaceId, int projectId, int requestId, [FromBody] AssignBespokeReporterDto dto)
        {
            var result = await _bespokeService.AssignReporterAsync(
                workspaceId, projectId, GetUserId(), requestId, dto.ReporterId);
            if (result == null) return BadRequest(new { message = "Chỉ Admin mới giao được Reporter." });
            return Ok(result);
        }

        [HttpPost("{projectId}/bespoke/{requestId}/start")]
        public async Task<IActionResult> StartBespokeWork(int workspaceId, int projectId, int requestId)
        {
            var result = await _bespokeService.StartWorkAsync(workspaceId, projectId, GetUserId(), requestId);
            if (result == null) return BadRequest(new { message = "Không thể bắt đầu xử lý yêu cầu này." });
            return Ok(result);
        }

        [HttpPost("{projectId}/bespoke/{requestId}/deliver")]
        public async Task<IActionResult> DeliverBespokeReport(int workspaceId, int projectId, int requestId)
        {
            var result = await _bespokeService.DeliverReportAsync(workspaceId, projectId, GetUserId(), requestId);
            if (result == null) return BadRequest(new { message = "Không thể nộp báo cáo." });
            return Ok(result);
        }

        [HttpGet("{projectId}/bespoke/{requestId}/download")]
        public async Task<IActionResult> DownloadBespokeReport(int workspaceId, int projectId, int requestId)
        {
            var file = await _bespokeService.DownloadDeliverableAsync(workspaceId, projectId, GetUserId(), requestId);
            if (file == null) return NotFound(new { message = "Chưa có file báo cáo." });
            return File(file.Value.Content, "text/html; charset=utf-8", file.Value.FileName);
        }
    }
}