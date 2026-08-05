using System.Security.Claims;
using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MCFH.Controllers.Admin;

[ApiController]
[Route("api/admin/scrape-packages")]
[Authorize]
public class AdminScrapePackageController : ControllerBase
{
    private readonly ScrapePackageAdminService _service;

    public AdminScrapePackageController(ScrapePackageAdminService service)
    {
        _service = service;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        return Ok(await _service.ListAsync(GetUserId()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertScrapePackageDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            if (result == null)
                return Forbid();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{packageId}")]
    public async Task<IActionResult> Update(int packageId, [FromBody] UpsertScrapePackageDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var result = await _service.UpdateAsync(GetUserId(), packageId, dto);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy gói hoặc không có quyền Admin." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{packageId}")]
    public async Task<IActionResult> Delete(int packageId)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        try
        {
            var ok = await _service.DeleteAsync(GetUserId(), packageId);
            if (!ok)
                return NotFound(new { message = "Không tìm thấy gói hoặc không có quyền Admin." });

            return Ok(new { message = "Đã xóa gói scrape." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
