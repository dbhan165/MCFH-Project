using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Projects;

[ApiController]
[Route("api/workspaces/{workspaceId}/projects")]
[Authorize]
public class ProjectCreateExtendedController : ControllerBase
{
    private readonly ProjectCreateExtendedService _service;

    public ProjectCreateExtendedController(McfhDbContext db)
    {
        _service = new ProjectCreateExtendedService(db);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("extended")]
    public async Task<IActionResult> ListExtended(int workspaceId)
    {
        var result = await _service.ListAsync(workspaceId, GetUserId());
        return Ok(result);
    }

    [HttpPost("with-sources")]
    public async Task<IActionResult> CreateWithSources(int workspaceId, [FromBody] CreateProjectExtendedDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.CreateAsync(workspaceId, GetUserId(), dto);
        if (result == null)
            return BadRequest(new { message = "Không thể tạo project. Bạn cần quyền Owner hoặc Editor." });

        return CreatedAtAction(nameof(GetById),
            new { workspaceId, projectId = result.ProjectId }, result);
    }

    [HttpGet("{projectId}/detail")]
    public async Task<IActionResult> GetById(int workspaceId, int projectId)
    {
        var result = await _service.GetByIdAsync(workspaceId, projectId, GetUserId());
        if (result == null)
            return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

        return Ok(result);
    }

    [HttpPut("{projectId}/extended")]
    public async Task<IActionResult> UpdateExtended(
        int workspaceId, int projectId, [FromBody] UpdateProjectExtendedDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.UpdateAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null)
            return BadRequest(new { message = "Không thể cập nhật project." });

        return Ok(result);
    }
}
