using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services
{
    public class ProjectService : IProjectService
    {
        private readonly McfhDbContext _context;

        public ProjectService(McfhDbContext context)
        {
            _context = context;
        }

        // ── Helper: kiểm tra user có phải member không ──
        private async Task<bool> IsMemberAsync(int workspaceId, int userId)
        {
            return await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        }

        // ── Helper: kiểm tra user có quyền Edit không (Owner hoặc Editor) ──
        private async Task<bool> CanEditAsync(int workspaceId, int userId)
        {
            return await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId &&
                               m.UserId == userId &&
                               (m.Role.RoleName == "Owner" || m.Role.RoleName == "Editor"));
        }

        // ── Helper: ghi activity log ──
        private async Task LogActivityAsync(int workspaceId, int userId, string actionType,
            string? targetType = null, int? targetId = null,
            string? targetName = null, string? description = null)
        {
            _context.WorkspaceActivityLogs.Add(new WorkspaceActivityLog
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                ActionType = actionType,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                Description = description,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        // Xem danh sách project — tất cả members
        public async Task<List<ProjectResponseDto>> GetProjectsAsync(int workspaceId, int userId)
        {
            if (!await IsMemberAsync(workspaceId, userId))
                return new List<ProjectResponseDto>();

            return await _context.Projects
                .Where(p => p.WorkspaceId == workspaceId && p.IsDeleted != true)
                .Select(p => new ProjectResponseDto
                {
                    ProjectId = p.ProjectId,
                    WorkspaceId = p.WorkspaceId,
                    Name = p.Name,
                    Description = p.Description,
                    SearchQuery = p.SearchQuery,
                    DataSourceCount = p.DataSources.Count,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }

        // Xem chi tiết — tất cả members
        public async Task<ProjectResponseDto?> GetByIdAsync(int workspaceId, int projectId, int userId)
        {
            if (!await IsMemberAsync(workspaceId, userId))
                return null;

            return await _context.Projects
                .Where(p => p.ProjectId == projectId &&
                            p.WorkspaceId == workspaceId &&
                            p.IsDeleted != true)
                .Select(p => new ProjectResponseDto
                {
                    ProjectId = p.ProjectId,
                    WorkspaceId = p.WorkspaceId,
                    Name = p.Name,
                    Description = p.Description,
                    SearchQuery = p.SearchQuery,
                    DataSourceCount = p.DataSources.Count,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        // Tạo project — Owner + Editor
        public async Task<ProjectResponseDto?> CreateAsync(int workspaceId, int userId, CreateProjectDto dto)
        {
            if (!await CanEditAsync(workspaceId, userId))
                return null;

            var project = new Project
            {
                WorkspaceId = workspaceId,
                Name = dto.Name,
                Description = dto.Description,
                SearchQuery = dto.SearchQuery,
                CreatedAt = DateTime.Now
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, userId,
                actionType: "CREATE_PROJECT",
                targetType: "project",
                targetId: project.ProjectId,
                targetName: project.Name,
                description: $"Tạo project \"{project.Name}\"");

            return await GetByIdAsync(workspaceId, project.ProjectId, userId);
        }

        // Cập nhật project — Owner + Editor
        public async Task<ProjectResponseDto?> UpdateAsync(int workspaceId, int projectId, int userId, UpdateProjectDto dto)
        {
            if (!await CanEditAsync(workspaceId, userId))
                return null;

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId &&
                                          p.WorkspaceId == workspaceId &&
                                          p.IsDeleted != true);

            if (project == null) return null;

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.SearchQuery = dto.SearchQuery;
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, userId,
                actionType: "UPDATE_PROJECT",
                targetType: "project",
                targetId: projectId,
                targetName: project.Name,
                description: $"Cập nhật project \"{project.Name}\"");

            return await GetByIdAsync(workspaceId, projectId, userId);
        }

        // Xóa mềm project — Owner + Editor
        public async Task<bool> DeleteAsync(int workspaceId, int projectId, int userId)
        {
            if (!await CanEditAsync(workspaceId, userId))
                return false;

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId &&
                                          p.WorkspaceId == workspaceId &&
                                          p.IsDeleted != true);

            if (project == null) return false;

            project.IsDeleted = true;
            project.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await LogActivityAsync(workspaceId, userId,
                actionType: "DELETE_PROJECT",
                targetType: "project",
                targetId: projectId,
                targetName: project.Name,
                description: $"Xóa project \"{project.Name}\"");

            return true;
        }
    }
}