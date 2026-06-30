using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class ProjectCreateExtendedService
{
    private readonly McfhDbContext _context;

    public ProjectCreateExtendedService(McfhDbContext context)
    {
        _context = context;
    }

    private async Task<bool> CanEditAsync(int workspaceId, int userId) =>
        await _context.WorkspaceMembers.AnyAsync(m =>
            m.WorkspaceId == workspaceId &&
            m.UserId == userId &&
            (m.Role.RoleName == "Owner" || m.Role.RoleName == "Editor"));

    private async Task LogActivityAsync(
        int workspaceId, int userId, string actionType,
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

    public async Task<ProjectExtendedResponseDto?> CreateAsync(
        int workspaceId, int userId, CreateProjectExtendedDto dto)
    {
        if (!await CanEditAsync(workspaceId, userId))
            return null;

        var project = new Project
        {
            WorkspaceId = workspaceId,
            Name = dto.Name,
            Description = dto.Description,
            SearchQuery = dto.SearchQuery,
            EnableFacebook = dto.EnableFacebook,
            EnableYoutube = dto.EnableYoutube,
            EnableTiktok = dto.EnableTiktok,
            EnableMaps = dto.EnableMaps,
            CreatedAt = DateTime.Now
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        foreach (var source in dto.DataSources)
        {
            if (string.IsNullOrWhiteSpace(source.Platform)) continue;

            _context.DataSources.Add(new DataSource
            {
                ProjectId = project.ProjectId,
                Platform = source.Platform.ToLowerInvariant(),
                SourceType = string.IsNullOrWhiteSpace(source.TargetUrl) ? "keyword" : "url",
                TargetUrl = string.IsNullOrWhiteSpace(source.TargetUrl) ? null : source.TargetUrl.Trim(),
                SearchQuery = dto.SearchQuery,
                Status = "active"
            });
        }

        if (dto.DataSources.Count > 0)
            await _context.SaveChangesAsync();

        await LogActivityAsync(workspaceId, userId,
            actionType: "CREATE_PROJECT",
            targetType: "project",
            targetId: project.ProjectId,
            targetName: project.Name,
            description: $"Tạo project \"{project.Name}\"");

        return await GetByIdAsync(workspaceId, project.ProjectId, userId);
    }

    public async Task<List<ProjectExtendedResponseDto>> ListAsync(int workspaceId, int userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return new();

        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && p.IsDeleted != true)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectExtendedResponseDto
            {
                ProjectId = p.ProjectId,
                WorkspaceId = p.WorkspaceId,
                Name = p.Name,
                Description = p.Description,
                SearchQuery = p.SearchQuery,
                DataSourceCount = p.DataSources.Count,
                EnableFacebook = p.EnableFacebook == true,
                EnableYoutube = p.EnableYoutube == true,
                EnableTiktok = p.EnableTiktok == true,
                EnableMaps = p.EnableMaps == true,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ProjectExtendedResponseDto?> UpdateAsync(
        int workspaceId, int projectId, int userId, UpdateProjectExtendedDto dto)
    {
        if (!await CanEditAsync(workspaceId, userId))
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p =>
                p.ProjectId == projectId &&
                p.WorkspaceId == workspaceId &&
                p.IsDeleted != true);
        if (project == null) return null;

        project.Name = dto.Name.Trim();
        project.Description = dto.Description;
        project.SearchQuery = dto.SearchQuery;
        if (dto.EnableFacebook.HasValue) project.EnableFacebook = dto.EnableFacebook.Value;
        if (dto.EnableYoutube.HasValue) project.EnableYoutube = dto.EnableYoutube.Value;
        if (dto.EnableTiktok.HasValue) project.EnableTiktok = dto.EnableTiktok.Value;
        if (dto.EnableMaps.HasValue) project.EnableMaps = dto.EnableMaps.Value;

        await _context.SaveChangesAsync();

        await LogActivityAsync(workspaceId, userId,
            actionType: "UPDATE_PROJECT",
            targetType: "project",
            targetId: projectId,
            targetName: project.Name,
            description: $"Cập nhật project \"{project.Name}\"");

        return await GetByIdAsync(workspaceId, projectId, userId);
    }

    public async Task<ProjectExtendedResponseDto?> GetByIdAsync(int workspaceId, int projectId, int userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return null;

        return await _context.Projects
            .Where(p => p.ProjectId == projectId &&
                        p.WorkspaceId == workspaceId &&
                        p.IsDeleted != true)
            .Select(p => new ProjectExtendedResponseDto
            {
                ProjectId = p.ProjectId,
                WorkspaceId = p.WorkspaceId,
                Name = p.Name,
                Description = p.Description,
                SearchQuery = p.SearchQuery,
                DataSourceCount = p.DataSources.Count,
                EnableFacebook = p.EnableFacebook == true,
                EnableYoutube = p.EnableYoutube == true,
                EnableTiktok = p.EnableTiktok == true,
                EnableMaps = p.EnableMaps == true,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();
    }
}
