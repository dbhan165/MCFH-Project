using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin/platform-cookies")]
[Authorize]
public class AdminPlatformCookieController : ControllerBase
{
    private readonly PlatformCookieAdminService _cookieAdmin;

    public AdminPlatformCookieController(PlatformCookieAdminService cookieAdmin)
    {
        _cookieAdmin = cookieAdmin;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        return Ok(await _cookieAdmin.ListAsync(GetUserId()));
    }

    [HttpGet("{platform}")]
    public async Task<IActionResult> Get(string platform)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var result = await _cookieAdmin.GetAsync(GetUserId(), platform);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy platform hoặc không có quyền Admin." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{platform}")]
    public async Task<IActionResult> UpdateMeta(string platform, [FromBody] UpdatePlatformCookieMetaDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var result = await _cookieAdmin.UpdateMetaAsync(GetUserId(), platform, dto);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy platform hoặc không có quyền Admin." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{platform}/content")]
    public async Task<IActionResult> UpdateContent(string platform, [FromBody] UpdatePlatformCookieContentDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var result = await _cookieAdmin.UpdateContentAsync(GetUserId(), platform, dto);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy platform hoặc không có quyền Admin." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{platform}/content")]
    public async Task<IActionResult> ClearContent(string platform)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var ok = await _cookieAdmin.ClearContentAsync(GetUserId(), platform);
            if (!ok)
                return NotFound(new { message = "Không tìm thấy platform hoặc không có quyền Admin." });

            return Ok(new { message = $"Đã xóa nội dung cookie {platform}.", platform, status = "disabled" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
