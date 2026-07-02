using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin/fb-sources")]
[Authorize]
public class AdminFbSourceController : ControllerBase
{
    private readonly FbSourceAdminService _fbSourceAdmin;

    public AdminFbSourceController(FbSourceAdminService fbSourceAdmin)
    {
        _fbSourceAdmin = fbSourceAdmin;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        return Ok(await _fbSourceAdmin.ListAsync(GetUserId()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertFbSourceDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.GroupUrl))
            return BadRequest(new { message = "URL group Facebook là bắt buộc." });

        try
        {
            var result = await _fbSourceAdmin.CreateAsync(GetUserId(), dto);
            if (result == null)
                return Forbid();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{fbSourceId}")]
    public async Task<IActionResult> Update(int fbSourceId, [FromBody] UpsertFbSourceDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.GroupUrl))
            return BadRequest(new { message = "URL group Facebook là bắt buộc." });

        try
        {
            var result = await _fbSourceAdmin.UpdateAsync(GetUserId(), fbSourceId, dto);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy nguồn hoặc không có quyền Admin." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{fbSourceId}")]
    public async Task<IActionResult> Delete(int fbSourceId)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        var ok = await _fbSourceAdmin.DeleteAsync(GetUserId(), fbSourceId);
        if (!ok)
            return NotFound(new { message = "Không tìm thấy nguồn hoặc không có quyền Admin." });

        return Ok(new { message = "Đã xóa nguồn Facebook." });
    }
}
