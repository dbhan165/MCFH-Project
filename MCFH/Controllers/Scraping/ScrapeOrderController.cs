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

    public ScrapeOrderController(
        McfhDbContext db,
        ScrapeJobRunner jobRunner,
        IServiceScopeFactory scopeFactory,
        IOptions<ScrapeOptions> scrapeOptions)
    {
        _service = new ScrapeOrderService(db, jobRunner, scopeFactory, scrapeOptions);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim) : null;
    }

    [HttpPost("quote")]
    public IActionResult GetQuote([FromBody] ScrapeQuoteRequestDto dto)
    {
        return Ok(_service.GetQuote(dto.PostedSinceDays));
    }

    [HttpGet("quote")]
    public IActionResult GetQuoteByQuery([FromQuery] int postedSinceDays = 30)
    {
        return Ok(_service.GetQuote(postedSinceDays));
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

    [HttpPost("{orderId:int}/pay")]
    public async Task<IActionResult> Pay(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.PayOrderAsync(userId.Value, orderId);
        if (result == null) return BadRequest(new { message = "Thanh toán thất bại hoặc đơn không hợp lệ." });
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
}
