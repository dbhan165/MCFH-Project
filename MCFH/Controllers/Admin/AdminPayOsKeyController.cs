using MCFH.DTOs.Admin.ProviderKeys;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin/payos-keys")]
[Authorize]
public class AdminPayOsKeyController : ControllerBase
{
    private readonly ProviderKeyAdminService _service;
    private readonly ILogger<AdminPayOsKeyController> _logger;

    public AdminPayOsKeyController(
        ProviderKeyAdminService service,
        ILogger<AdminPayOsKeyController> logger)
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
            return Ok(await _service.ListPayOsAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/admin/payos-keys thất bại");
            return StatusCode(500, new { message = "Không thể tải danh sách PayOS key.", detail = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        if (GetUserId() <= 0) return Unauthorized();
        var result = await _service.GetPayOsAsync(GetUserId(), id);
        return result == null
            ? NotFound(new { message = "Không tìm thấy PayOS key." })
            : Ok(result);
    }

    [HttpGet("{id:int}/reveal")]
    public async Task<IActionResult> Reveal(int id)
    {
        if (GetUserId() <= 0) return Unauthorized();
        var result = await _service.RevealPayOsAsync(GetUserId(), id);
        return result == null
            ? NotFound(new { message = "Không tìm thấy PayOS key." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayOsKeyDto dto)
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            var result = await _service.CreatePayOsAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(Get), new { id = result.PayOsKeyId }, result);
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
            _logger.LogError(ex, "POST /api/admin/payos-keys thất bại");
            return StatusCode(500, new { message = "Không thể tạo PayOS key.", detail = ex.Message });
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePayOsKeyDto dto)
    {
        if (GetUserId() <= 0) return Unauthorized();
        try
        {
            var result = await _service.UpdatePayOsAsync(GetUserId(), id, dto);
            return result == null
                ? NotFound(new { message = "Không tìm thấy PayOS key." })
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
            var ok = await _service.DeletePayOsAsync(GetUserId(), id);
            return ok
                ? Ok(new { message = "Đã xóa PayOS key.", id })
                : NotFound(new { message = "Không tìm thấy PayOS key." });
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
