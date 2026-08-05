using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Workspaces;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspaceExtendedController : ControllerBase
{
    private readonly WorkspaceBootstrapService _bootstrap;
    private readonly BespokeReportService _bespoke;

    public WorkspaceExtendedController(McfhDbContext db, BespokeReportService bespoke)
    {
        _bootstrap = new WorkspaceBootstrapService(db);
        _bespoke = bespoke;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Khởi tạo WORKSPACE_CREDITS sau khi tạo workspace (gọi từ FE nếu cần).
    /// </summary>
    [HttpPost("{workspaceId}/bootstrap")]
    public async Task<IActionResult> BootstrapWorkspace(int workspaceId)
    {
        if (!await _bootstrap.IsMemberAsync(workspaceId, GetUserId()))
            return Forbid();

        var ok = await _bootstrap.EnsureCreditsAsync(workspaceId);
        if (!ok) return NotFound(new { message = "Workspace không tồn tại." });

        return Ok(new { message = "Đã khởi tạo tài nguyên workspace." });
    }

    /// <summary>
    /// Tạo báo cáo chuyên sâu: tự tạo Project mới trong workspace (không gắn project có sẵn).
    /// Response có projectId để FE gọi pay/download trên project vừa tạo.
    /// </summary>
    [HttpPost("{workspaceId}/bespoke")]
    public async Task<IActionResult> CreateBespokeStandalone(int workspaceId, [FromBody] CreateBespokeRequestDto dto)
    {
        var result = await _bespoke.CreateStandaloneRequestAsync(workspaceId, GetUserId(), dto);
        if (result == null)
            return BadRequest(new { message = "Không tạo được yêu cầu. Kiểm tra quyền Owner/Editor và thông tin (title, keyword)." });
        return Ok(result);
    }
}
