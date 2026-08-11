using MCFH.DTOs.ProjectDtos;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MCFH.Controllers.Projects;

[ApiController]
[Route("api/workspaces/{workspaceId}/projects/{projectId}")]
[Authorize]
public class ProjectDataSourceController : ControllerBase
{
    private readonly ProjectDataSourceService _service;

    public ProjectDataSourceController(ProjectDataSourceService service)
    {
        _service = service;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("data-sources")]
    public async Task<IActionResult> GetDataSources(int workspaceId, int projectId)
    {
        var sources = await _service.GetDataSourcesAsync(workspaceId, projectId, GetUserId());
        return Ok(sources);
    }

    [HttpPost("data-sources")]
    public async Task<IActionResult> AddDataSource(int workspaceId, int projectId, [FromBody] CreateProjectDataSourceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.AddDataSourceAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null)
            return Forbid();

        return Ok(result);
    }

    [HttpPut("data-sources/{sourceId}/toggle")]
    public async Task<IActionResult> ToggleDataSource(int workspaceId, int projectId, int sourceId)
    {
        var success = await _service.ToggleDataSourceStatusAsync(workspaceId, projectId, sourceId, GetUserId());
        if (!success)
            return BadRequest(new { message = "Không thể cập nhật trạng thái nguồn dữ liệu." });

        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("data-sources/{sourceId}")]
    public async Task<IActionResult> DeleteDataSource(int workspaceId, int projectId, int sourceId)
    {
        var success = await _service.DeleteDataSourceAsync(workspaceId, projectId, sourceId, GetUserId());
        if (!success)
            return BadRequest(new { message = "Không thể xóa nguồn dữ liệu." });

        return Ok(new { message = "Xóa thành công." });
    }

    [HttpGet("imports")]
    public async Task<IActionResult> GetImports(int workspaceId, int projectId)
    {
        var imports = await _service.GetImportFilesAsync(workspaceId, projectId, GetUserId());
        return Ok(imports);
    }

    [HttpPost("imports")]
    public async Task<IActionResult> CreateImport(int workspaceId, int projectId, [FromForm] CreateImportFileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.CreateImportFileAsync(workspaceId, projectId, GetUserId(), dto);
        if (result == null)
            return Forbid();

        return Ok(result);
    }

    [HttpDelete("imports/{fileId}")]
    public async Task<IActionResult> DeleteImportFile(int workspaceId, int projectId, int fileId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.DeleteImportFileAsync(workspaceId, projectId, fileId, userId);
        if (!result)
            return NotFound(new { message = "Không tìm thấy file import hoặc không có quyền." });

        return Ok(new { message = "Xóa file import thành công." });
    }
}
