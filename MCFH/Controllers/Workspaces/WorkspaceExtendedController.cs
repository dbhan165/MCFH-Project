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

    public WorkspaceExtendedController(McfhDbContext db) => _bootstrap = new WorkspaceBootstrapService(db);

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
}
