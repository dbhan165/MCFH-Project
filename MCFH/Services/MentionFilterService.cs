using System.Text.Json;
using MCFH.DTOs;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class MentionFilterService
{
    private readonly McfhDbContext _context;
    private readonly ProjectAnalyticsService _analytics;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public MentionFilterService(McfhDbContext context, ProjectAnalyticsService analytics)
    {
        _context = context;
        _analytics = analytics;
    }

    public async Task<List<SavedFilterDto>> ListSavedFiltersAsync(int workspaceId, int projectId, int userId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return new();

        var rows = await _context.SavedFilters
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return rows.Select(MapFilter).ToList();
    }

    public async Task<SavedFilterDto?> CreateSavedFilterAsync(
        int workspaceId, int projectId, int userId, CreateSavedFilterDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;

        var entity = new SavedFilter
        {
            ProjectId = projectId,
            Name = dto.Name.Trim(),
            FilterConfig = JsonSerializer.Serialize(dto.Config, JsonOptions),
            CreatedBy = userId,
            CreatedAt = DateTime.Now
        };
        _context.SavedFilters.Add(entity);
        await _context.SaveChangesAsync();
        return MapFilter(entity);
    }

    public async Task<SavedFilterDto?> UpdateSavedFilterAsync(
        int workspaceId, int projectId, int userId, int filterId, UpdateSavedFilterDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;

        var entity = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.FilterId == filterId && f.ProjectId == projectId);
        if (entity == null) return null;

        entity.Name = dto.Name.Trim();
        entity.FilterConfig = JsonSerializer.Serialize(dto.Config, JsonOptions);
        await _context.SaveChangesAsync();
        return MapFilter(entity);
    }

    public async Task<bool> DeleteSavedFilterAsync(int workspaceId, int projectId, int userId, int filterId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return false;

        var entity = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.FilterId == filterId && f.ProjectId == projectId);
        if (entity == null) return false;

        _context.SavedFilters.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> CanAccessProjectAsync(int workspaceId, int projectId, int userId)
    {
        var project = await _analytics.GetOverviewAsync(workspaceId, projectId, userId);
        return project != null;
    }

    private static SavedFilterDto MapFilter(SavedFilter entity)
    {
        MentionQueryDto config;
        try
        {
            config = JsonSerializer.Deserialize<MentionQueryDto>(entity.FilterConfig, JsonOptions) ?? new();
        }
        catch
        {
            config = new MentionQueryDto();
        }

        return new SavedFilterDto
        {
            FilterId = entity.FilterId,
            ProjectId = entity.ProjectId,
            Name = entity.Name,
            Config = config,
            CreatedAt = entity.CreatedAt
        };
    }
}
