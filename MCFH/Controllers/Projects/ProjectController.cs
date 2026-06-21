using MCFH.DTOs.ProjectDtos;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Projects
{
    [ApiController]
    [Route("api/workspaces/{workspaceId}/projects")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim!);
        }

        // GET api/workspaces/1/projects
        [HttpGet]
        public async Task<IActionResult> GetProjects(int workspaceId)
        {
            var userId = GetUserId();
            var result = await _projectService.GetProjectsAsync(workspaceId, userId);
            return Ok(result);
        }

        // GET api/workspaces/1/projects/5
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetById(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var result = await _projectService.GetByIdAsync(workspaceId, projectId, userId);

            if (result == null)
                return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // POST api/workspaces/1/projects
        [HttpPost]
        public async Task<IActionResult> Create(int workspaceId, [FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _projectService.CreateAsync(workspaceId, userId, dto);

            if (result == null)
                return BadRequest(new { message = "Không thể tạo project. Bạn cần quyền Owner hoặc Editor." });

            return CreatedAtAction(nameof(GetById),
                new { workspaceId, projectId = result.ProjectId }, result);
        }

        // PUT api/workspaces/1/projects/5
        [HttpPut("{projectId}")]
        public async Task<IActionResult> Update(int workspaceId, int projectId, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _projectService.UpdateAsync(workspaceId, projectId, userId, dto);

            if (result == null)
                return BadRequest(new { message = "Không thể cập nhật project. Bạn cần quyền Owner hoặc Editor, hoặc project không tồn tại." });

            return Ok(result);
        }

        // DELETE api/workspaces/1/projects/5
        [HttpDelete("{projectId}")]
        public async Task<IActionResult> Delete(int workspaceId, int projectId)
        {
            var userId = GetUserId();
            var success = await _projectService.DeleteAsync(workspaceId, projectId, userId);

            if (!success)
                return BadRequest(new { message = "Không thể xóa project. Bạn cần quyền Owner hoặc Editor, hoặc project không tồn tại." });

            return Ok(new { message = "Xóa project thành công" });
        }
    }
}