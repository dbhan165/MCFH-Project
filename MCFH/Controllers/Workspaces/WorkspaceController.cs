using MCFH.DTOs.WorkspaceDtos;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Workspaces
{
    [ApiController]
    [Route("api/workspaces")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        // Lấy userId từ JWT token
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim!);
        }

        // ════════════════════════════════
        // WORKSPACE CRUD
        // ════════════════════════════════

        // GET api/workspaces
        [HttpGet]
        public async Task<IActionResult> GetMyWorkspaces()
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetMyWorkspacesAsync(userId);
            return Ok(result);
        }

        // GET api/workspaces/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetByIdAsync(id, userId);

            if (result == null)
                return NotFound(new { message = "Workspace không tồn tại hoặc bạn không có quyền truy cập" });

            return Ok(result);
        }

        // POST api/workspaces
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkspaceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _workspaceService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.WorkspaceId }, result);
        }

        // PUT api/workspaces/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkspaceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _workspaceService.UpdateAsync(id, userId, dto);

            if (result == null)
                return NotFound(new { message = "Workspace không tồn tại hoặc bạn không phải Owner" });

            return Ok(result);
        }

        // DELETE api/workspaces/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var success = await _workspaceService.DeleteAsync(id, userId);

            if (!success)
                return NotFound(new { message = "Workspace không tồn tại hoặc bạn không phải Owner" });

            return Ok(new { message = "Xóa workspace thành công" });
        }

        // ════════════════════════════════
        // MEMBER MANAGEMENT
        // ════════════════════════════════

        // GET api/workspaces/5/members
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetMembersAsync(id, userId);
            return Ok(result);
        }

        // POST api/workspaces/5/members/invite
        [HttpPost("{id}/members/invite")]
        public async Task<IActionResult> InviteMember(int id, [FromBody] InviteMemberDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var success = await _workspaceService.InviteMemberAsync(id, userId, dto);

            if (!success)
                return BadRequest(new { message = "Không thể gửi lời mời. Email đã được mời hoặc đã là thành viên." });

            return Ok(new { message = "Đã gửi lời mời thành công, chờ Owner duyệt." });
        }

        // GET api/workspaces/5/members/invitations
        [HttpGet("{id}/members/invitations")]
        public async Task<IActionResult> GetPendingInvitations(int id)
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetPendingInvitationsAsync(id, userId);
            return Ok(result);
        }

        // PUT api/workspaces/5/members/invitations/3/approve
        [HttpPut("{id}/members/invitations/{invitationId}/approve")]
        public async Task<IActionResult> ApproveInvitation(int id, int invitationId)
        {
            var userId = GetUserId();
            var success = await _workspaceService.ApproveInvitationAsync(id, userId, invitationId);

            if (!success)
                return BadRequest(new { message = "Không thể duyệt lời mời. Bạn không phải Owner hoặc lời mời không tồn tại." });

            return Ok(new { message = "Đã duyệt lời mời thành công." });
        }

        // PUT api/workspaces/5/members/invitations/3/reject
        [HttpPut("{id}/members/invitations/{invitationId}/reject")]
        public async Task<IActionResult> RejectInvitation(int id, int invitationId)
        {
            var userId = GetUserId();
            var success = await _workspaceService.RejectInvitationAsync(id, userId, invitationId);

            if (!success)
                return BadRequest(new { message = "Không thể từ chối lời mời." });

            return Ok(new { message = "Đã từ chối lời mời." });
        }

        // PUT api/workspaces/5/members/3/role
        [HttpPut("{id}/members/{targetUserId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, int targetUserId, [FromBody] UpdateMemberRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var success = await _workspaceService.UpdateMemberRoleAsync(id, userId, targetUserId, dto);

            if (!success)
                return BadRequest(new { message = "Không thể đổi role. Bạn không phải Owner hoặc member không tồn tại." });

            return Ok(new { message = "Đã cập nhật role thành công." });
        }

        // DELETE api/workspaces/5/members/3
        [HttpDelete("{id}/members/{targetUserId}")]
        public async Task<IActionResult> KickMember(int id, int targetUserId)
        {
            var userId = GetUserId();
            var success = await _workspaceService.KickMemberAsync(id, userId, targetUserId);

            if (!success)
                return BadRequest(new { message = "Không thể kick member. Bạn không phải Owner hoặc member không tồn tại." });

            return Ok(new { message = "Đã kick member thành công." });
        }

        // ════════════════════════════════
        // ACTIVITY LOG
        // ════════════════════════════════

        // GET api/workspaces/5/activity-logs?page=1&pageSize=20
        [HttpGet("{id}/activity-logs")]
        public async Task<IActionResult> GetActivityLogs(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetActivityLogsAsync(id, userId, page, pageSize);
            return Ok(result);
        }
    }
}