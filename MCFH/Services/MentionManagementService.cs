using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class MentionManagementService
{
    private readonly McfhDbContext _context;

    private static readonly HashSet<string> AllowedSentiments =
        new(StringComparer.OrdinalIgnoreCase) { "positive", "negative", "neutral" };

    private static readonly HashSet<string> AllowedMuteTypes =
        new(StringComparer.OrdinalIgnoreCase) { "author", "platform" };

    public MentionManagementService(McfhDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessProjectAsync(int workspaceId, int projectId, int userId) =>
        await _context.Projects.AnyAsync(p =>
            p.ProjectId == projectId &&
            p.WorkspaceId == workspaceId &&
            p.IsDeleted != true) &&
        await _context.WorkspaceMembers.AnyAsync(m =>
            m.WorkspaceId == workspaceId && m.UserId == userId);

    public async Task<List<MentionTagDto>> ListTagsAsync(int workspaceId, int projectId, int userId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId))
            return new List<MentionTagDto>();

        return await _context.Tags
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Name)
            .Select(t => new MentionTagDto
            {
                TagId = t.TagId,
                Name = t.Name,
                Color = t.Color
            })
            .ToListAsync();
    }

    public async Task<MentionTagDto?> CreateTagAsync(
        int workspaceId, int projectId, int userId, CreateMentionTagDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;
        if (string.IsNullOrWhiteSpace(dto.Name)) return null;

        var name = dto.Name.Trim();
        var exists = await _context.Tags.AnyAsync(t =>
            t.ProjectId == projectId && t.Name.ToLower() == name.ToLower());
        if (exists) return null;

        var tag = new Tag
        {
            ProjectId = projectId,
            Name = name,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#00B4D8" : dto.Color.Trim(),
            CreatedBy = userId
        };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return new MentionTagDto { TagId = tag.TagId, Name = tag.Name, Color = tag.Color };
    }

    public async Task<bool> AssignTagsAsync(
        int workspaceId, int projectId, int userId, int feedbackId, AssignMentionTagsDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return false;

        var feedback = await _context.ScrapedFeedbacks
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f =>
                f.FeedbackId == feedbackId &&
                f.ProjectId == projectId &&
                f.IsDeleted != true);
        if (feedback == null) return false;

        var tagIds = dto.TagIds?.Distinct().ToList() ?? new List<int>();
        var tags = tagIds.Count == 0
            ? new List<Tag>()
            : await _context.Tags
                .Where(t => t.ProjectId == projectId && tagIds.Contains(t.TagId))
                .ToListAsync();

        feedback.Tags.Clear();
        foreach (var tag in tags)
            feedback.Tags.Add(tag);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSentimentAsync(
        int workspaceId, int projectId, int userId, int feedbackId, UpdateMentionSentimentDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return false;

        var sentiment = dto.Sentiment?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(sentiment) || !AllowedSentiments.Contains(sentiment))
            return false;

        var feedback = await _context.ScrapedFeedbacks
            .Include(f => f.AiAnalysis)
            .FirstOrDefaultAsync(f =>
                f.FeedbackId == feedbackId &&
                f.ProjectId == projectId &&
                f.IsDeleted != true);
        if (feedback == null) return false;

        if (feedback.AiAnalysis == null)
        {
            feedback.AiAnalysis = new AiAnalysis
            {
                FeedbackId = feedback.FeedbackId,
                MainSentiment = sentiment,
                ConfidenceScore = 1,
                IsCrisisAlert = sentiment == "negative",
                SentimentOverrideBy = userId,
                ProcessedAt = DateTime.Now
            };
            _context.AiAnalyses.Add(feedback.AiAnalysis);
        }
        else
        {
            feedback.AiAnalysis.MainSentiment = sentiment;
            feedback.AiAnalysis.SentimentOverrideBy = userId;
            feedback.AiAnalysis.IsCrisisAlert = sentiment == "negative";
            feedback.AiAnalysis.ProcessedAt = DateTime.Now;
            feedback.AiAnalysis.ConfidenceScore = 1;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MuteEntityDto>> ListMutedAsync(int workspaceId, int projectId, int userId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId))
            return new List<MuteEntityDto>();

        return await _context.MutedEntities
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MuteEntityDto
            {
                MuteId = m.MuteId,
                EntityType = m.EntityType,
                EntityValue = m.EntityValue,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MuteEntityDto?> MuteSourceAsync(
        int workspaceId, int projectId, int userId, MuteMentionSourceDto dto)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return null;

        var entityType = dto.EntityType?.Trim().ToLowerInvariant();
        var entityValue = dto.EntityValue?.Trim();
        if (string.IsNullOrWhiteSpace(entityType) || !AllowedMuteTypes.Contains(entityType))
            return null;
        if (string.IsNullOrWhiteSpace(entityValue)) return null;

        var exists = await _context.MutedEntities.AnyAsync(m =>
            m.ProjectId == projectId &&
            m.EntityType.ToLower() == entityType &&
            m.EntityValue.ToLower() == entityValue.ToLower());
        if (exists) return null;

        var mute = new MutedEntity
        {
            ProjectId = projectId,
            EntityType = entityType,
            EntityValue = entityValue,
            MutedBy = userId,
            CreatedAt = DateTime.Now
        };
        _context.MutedEntities.Add(mute);
        await _context.SaveChangesAsync();

        return new MuteEntityDto
        {
            MuteId = mute.MuteId,
            EntityType = mute.EntityType,
            EntityValue = mute.EntityValue,
            CreatedAt = mute.CreatedAt
        };
    }

    public async Task<bool> UnmuteAsync(int workspaceId, int projectId, int userId, int muteId)
    {
        if (!await CanAccessProjectAsync(workspaceId, projectId, userId)) return false;

        var mute = await _context.MutedEntities
            .FirstOrDefaultAsync(m => m.MuteId == muteId && m.ProjectId == projectId);
        if (mute == null) return false;

        _context.MutedEntities.Remove(mute);
        await _context.SaveChangesAsync();
        return true;
    }
}
