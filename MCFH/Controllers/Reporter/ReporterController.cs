using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Reporter;

[ApiController]
[Route("api/reporter")]
[Authorize]
public class ReporterController : ControllerBase
{
    private readonly ReporterPortalService _reporter;

    public ReporterController(McfhDbContext db, IEmailService emailService, MCFH.Services.Scraping.ICommentBundleStorage bundleStorage)
    {
        var analytics = new ProjectAnalyticsService(db, bundleStorage);
        var bespoke = new BespokeReportService(db, analytics, emailService);
        _reporter = new ReporterPortalService(db, bespoke, analytics);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanban()
    {
        var result = await _reporter.GetKanbanAsync(GetUserId());
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("requests/{requestId}")]
    public async Task<IActionResult> GetRequest(int requestId)
    {
        var result = await _reporter.GetRequestAsync(GetUserId(), requestId);
        if (result == null) return NotFound(new { message = "Không tìm thấy yêu cầu." });
        return Ok(result);
    }

    [HttpPost("requests/{requestId}/quote")]
    public async Task<IActionResult> Quote(int requestId, [FromBody] QuoteBespokeDto dto)
    {
        var result = await _reporter.QuoteAsync(GetUserId(), requestId, dto);
        if (result == null) return BadRequest(new { message = "Không thể gửi báo giá. Hạn bàn giao phải từ hôm nay trở đi." });
        return Ok(result);
    }

    [HttpPost("requests/{requestId}/start")]
    public async Task<IActionResult> StartWork(int requestId)
    {
        var result = await _reporter.StartWorkAsync(GetUserId(), requestId);
        if (result == null) return BadRequest(new { message = "Không thể bắt đầu làm báo cáo." });
        return Ok(result);
    }

    [HttpPost("requests/{requestId}/deliver")]
    public async Task<IActionResult> Deliver(int requestId)
    {
        var result = await _reporter.DeliverAsync(GetUserId(), requestId);
        if (result == null) return BadRequest(new { message = "Không thể nộp báo cáo." });
        return Ok(result);
    }

    [HttpGet("requests/{requestId}/download")]
    public async Task<IActionResult> Download(int requestId)
    {
        var file = await _reporter.DownloadAsync(GetUserId(), requestId);
        if (file == null) return NotFound(new { message = "Không tìm thấy báo cáo." });
        var contentType = BespokeReportService.GetDeliverableContentType(file.Value.FileName);
        return File(file.Value.Content, contentType, file.Value.FileName);
    }

    [HttpPost("requests/{requestId}/upload-revision")]
    public async Task<IActionResult> UploadRevision(int requestId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file báo cáo." });

        await using var stream = file.OpenReadStream();
        var result = await _reporter.UploadRevisionAsync(GetUserId(), requestId, stream, file.FileName);
        if (result == null) return BadRequest(new { message = "Không thể upload. Chỉ nhận đơn đang được giao / đang xử lý / chờ sửa đổi." });
        return Ok(result);
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance()
    {
        var result = await _reporter.GetPerformanceAsync(GetUserId());
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("requests/{requestId}/analytics-preview")]
    public async Task<IActionResult> GetAnalyticsPreview(int requestId)
    {
        var result = await _reporter.GetRequestAnalyticsOverviewAsync(GetUserId(), requestId);
        if (result == null) return NotFound(new { message = "Không có dữ liệu analytics." });
        return Ok(result);
    }
}
