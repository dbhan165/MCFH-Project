using MCFH.DTOs.Admin.ProviderKeys;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin/brevo-keys")]
[Authorize]
public class AdminBrevoKeyController : ControllerBase
{
    private readonly ProviderKeyAdminService _service;
    private readonly ILogger<AdminBrevoKeyController> _logger;

    public AdminBrevoKeyController(
        ProviderKeyAdminService service,
        ILogger<AdminBrevoKeyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            return Ok(await _service.ListBrevoAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/admin/brevo-keys thất bại");
            return StatusCode(500, new { message = "Không thể tải danh sách Brevo key.", detail = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        if (GetUserId() <= 0) return Unauthorized();
        var result = await _service.GetBrevoAsync(GetUserId(), id);
        return result == null
            ? NotFound(new { message = "Không tìm thấy Brevo key." })
            : Ok(result);
    }

    [HttpGet("{id:int}/reveal")]
    public async Task<IActionResult> Reveal(int id)
    {
        if (GetUserId() <= 0) return Unauthorized();
        var result = await _service.RevealBrevoAsync(GetUserId(), id);
        return result == null
            ? NotFound(new { message = "Không tìm thấy Brevo key." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrevoKeyDto dto)
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            var result = await _service.CreateBrevoAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(Get), new { id = result.BrevoKeyId }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST /api/admin/brevo-keys thất bại");
            return StatusCode(500, new { message = "Không thể tạo Brevo key.", detail = ex.Message });
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBrevoKeyDto dto)
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            var result = await _service.UpdateBrevoAsync(GetUserId(), id, dto);
            return result == null
                ? NotFound(new { message = "Không tìm thấy Brevo key." })
                : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            var ok = await _service.DeleteBrevoAsync(GetUserId(), id);
            return ok
                ? Ok(new { message = "Đã xóa Brevo key.", id })
                : NotFound(new { message = "Không tìm thấy Brevo key." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
