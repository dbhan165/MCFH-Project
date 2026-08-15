using MCFH.Configuration;
using MCFH.DTOs;
using MCFH.Models;
using MCFH.Services;
using MCFH.Services.Scraping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace MCFH.Controllers.Scraping;

[ApiController]
[Route("api/scrape-orders")]
[Authorize]
public class ScrapeOrderController : ControllerBase
{
    private readonly ScrapeOrderService _service;

    public ScrapeOrderController(ScrapeOrderService service)
    {
        _service = service;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim) : null;
    }

    [HttpPost("quote")]
    public async Task<IActionResult> GetQuote([FromBody] ScrapeQuoteRequestDto dto)
    {
        var package = MentionPackageTypes.Normalize(dto.MentionsPackage);
        if (package == null) return BadRequest(new { message = "Mentions package không hợp lệ." });

        Project? project = null;
        // Optional: frontend có thể gửi kèm projectId trong body để tính remaining quota
        // (hiện tại controller chưa nhận — frontend sẽ tự gọi /api/projects/{id}/mentions-quota)
        var quote = await _service.GetQuoteAsync(package, project);
        if (quote == null) return NotFound(new { message = "Gói không tồn tại hoặc đã ngừng bán." });
        return Ok(quote);
    }

    [HttpGet("quote")]
    public async Task<IActionResult> GetQuoteByQuery([FromQuery] string mentionsPackage = "PACK_100")
    {
        var package = MentionPackageTypes.Normalize(mentionsPackage);
        if (package == null) return BadRequest(new { message = "Mentions package không hợp lệ." });
        var quote = await _service.GetQuoteAsync(package);
        if (quote == null) return NotFound(new { message = "Gói không tồn tại hoặc đã ngừng bán." });
        return Ok(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScrapeOrderDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.CreateOrderAsync(userId.Value, dto);
        if (result == null) return BadRequest(new { message = "Không thể tạo đơn cào dữ liệu." });
        return Ok(result);
    }

    /// <summary>Tạo checkout PayOS — trả về checkoutUrl / qrCode để frontend redirect.</summary>
    [HttpPost("{orderId:int}/pay")]
    public async Task<IActionResult> Pay(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.PayOrderAsync(userId.Value, orderId);
        if (result == null) return BadRequest(new { message = "Không tạo được thanh toán hoặc đơn không hợp lệ." });
        return Ok(result);
    }

    /// <summary>
    /// Trang return sau thanh toán gọi endpoint này — server tra cứu lại PayOS/DB,
    /// không tin query param từ PayOS redirect.
    /// </summary>
    [HttpGet("{orderId:int}/payment-status")]
    public async Task<IActionResult> PaymentStatus(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.ConfirmPaymentAsync(userId.Value, orderId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> Get(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.GetOrderAsync(userId.Value, orderId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? workspaceId,
        [FromQuery] int? projectId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await _service.ListOrdersAsync(userId.Value, workspaceId, projectId));
    }

    /// <summary>
    /// Trả về quota mentions của Project (tổng/đã dùng/còn lại/full unlimited) + các package đang active.
    /// Mọi member workspace đều có thể xem.
    /// </summary>
    [HttpGet("project/{projectId:int}/mentions-quota")]
    public async Task<IActionResult> GetProjectMentionsQuota(int projectId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.GetProjectMentionsQuotaAsync(userId.Value, projectId);
        if (result == null) return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập." });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("force-scrape/{projectId:int}")]
    public IActionResult ForceScrape(int projectId)
    {
        var jobId = Guid.NewGuid().ToString("N");
        Hangfire.BackgroundJob.Enqueue<MCFH.Services.Scraping.ScrapeByKeywordService>(s => s.ScrapeAsync(projectId, null, null, false, jobId));
        
        // Cập nhật jobId vào SCRAPE_ORDERS
        var db = HttpContext.RequestServices.GetService<MCFH.Models.McfhDbContext>();
        var order = db.ScrapeOrders.OrderByDescending(o => o.OrderId).FirstOrDefault(o => o.ProjectId == projectId);
        if (order != null)
        {
            order.ScrapeJobId = jobId;
            order.Status = "scraping";
            order.ProgressPercent = 5;
            db.SaveChanges();
        }
        return Ok(new { message = "Enqueued", jobId = jobId });
    }
}
