using MCFH.DTOs.ProjectDtos;

namespace MCFH.Services
{
    public interface IProjectService
    {
        Task<List<ProjectResponseDto>> GetProjectsAsync(int workspaceId, int userId);
        Task<ProjectResponseDto?> GetByIdAsync(int workspaceId, int projectId, int userId);
        Task<ProjectResponseDto?> CreateAsync(int workspaceId, int userId, CreateProjectDto dto);
        Task<ProjectResponseDto?> UpdateAsync(int workspaceId, int projectId, int userId, UpdateProjectDto dto);
        Task<bool> DeleteAsync(int workspaceId, int projectId, int userId);
    }
}