using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

using ClosedXML.Excel;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

namespace MCFH.Services;

public class ProjectDataSourceService
{
    private readonly McfhDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProjectDataSourceService(McfhDbContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    private async Task<bool> HasAccessAsync(int workspaceId, int projectId, int userId)
    {
        return await _db.Projects
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.ProjectId == projectId && p.IsDeleted != true &&
                           p.Workspace.WorkspaceMembers.Any(m => m.UserId == userId));
    }

    public async Task<List<DataSourceDto>> GetDataSourcesAsync(int workspaceId, int projectId, int userId)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return new List<DataSourceDto>();

        var sources = await _db.DataSources
            .Where(s => s.ProjectId == projectId)
            .Select(s => new DataSourceDto
            {
                SourceId = s.SourceId,
                Platform = s.Platform,
                SourceType = s.SourceType,
                TargetUrl = s.TargetUrl,
                SearchQuery = s.SearchQuery,
                Status = s.Status
            })
            .ToListAsync();

        return sources;
    }

    public async Task<DataSourceDto?> AddDataSourceAsync(int workspaceId, int projectId, int userId, CreateProjectDataSourceDto dto)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return null;

        var source = new DataSource
        {
            ProjectId = projectId,
            Platform = dto.Platform,
            SourceType = dto.SourceType,
            TargetUrl = dto.TargetUrl,
            SearchQuery = dto.SearchQuery,
            Status = "active"
        };

        _db.DataSources.Add(source);
        await _db.SaveChangesAsync();

        return new DataSourceDto
        {
            SourceId = source.SourceId,
            Platform = source.Platform,
            SourceType = source.SourceType,
            TargetUrl = source.TargetUrl,
            SearchQuery = source.SearchQuery,
            Status = source.Status
        };
    }

    public async Task<bool> ToggleDataSourceStatusAsync(int workspaceId, int projectId, int sourceId, int userId)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return false;

        var source = await _db.DataSources.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.SourceId == sourceId);
        if (source == null)
            return false;

        source.Status = source.Status == "active" ? "paused" : "active";
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteDataSourceAsync(int workspaceId, int projectId, int sourceId, int userId)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return false;

        var source = await _db.DataSources.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.SourceId == sourceId);
        if (source == null)
            return false;

        _db.DataSources.Remove(source);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<ImportFileDto>> GetImportFilesAsync(int workspaceId, int projectId, int userId)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return new List<ImportFileDto>();

        var files = await _db.ImportFiles
            .Include(f => f.UploadedByNavigation)
            .Where(f => f.ProjectId == projectId)
            .Select(f => new ImportFileDto
            {
                FileId = f.FileId,
                SourceId = f.SourceId,
                FileName = f.FileName,
                FileUrl = f.FileUrl,
                TotalRows = f.TotalRows,
                ImportedRows = f.ImportedRows,
                Status = f.Status,
                ImportedAt = f.ImportedAt,
                UploadedByName = f.UploadedByNavigation.FullName
            })
            .OrderByDescending(f => f.FileId)
            .ToListAsync();

        return files;
    }

    public async Task<ImportFileDto?> CreateImportFileAsync(int workspaceId, int projectId, int userId, CreateImportFileDto dto)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return null;

        var importFile = new ImportFile
        {
            ProjectId = projectId,
            SourceId = dto.SourceId,
                    
            UploadedBy = userId,
            FileName = dto.File.FileName,
            FileUrl = "local://uploaded",
            Status = "processing",
            ImportedAt = System.DateTime.UtcNow
        };
        _db.ImportFiles.Add(importFile);
        await _db.SaveChangesAsync();

        int importedCount = 0;
        using var stream = dto.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet != null)
        {
            var rows = worksheet.RowsUsed().Skip(1); // Skip header
            foreach (var row in rows)
            {
                var author = row.Cell(1).GetString();
                var content = row.Cell(2).GetString();
                var timeStr = row.Cell(3).GetString();
                var platform = row.Cell(4).GetString();

                if (string.IsNullOrWhiteSpace(content)) continue;

                var feedback = new ScrapedFeedback
                {
                    ProjectId = projectId,
                    SourceId = dto.SourceId,
                    ImportFileId = importFile.FileId,
                    Platform = string.IsNullOrWhiteSpace(platform) ? "other" : platform.ToLower(),
                    AuthorName = string.IsNullOrWhiteSpace(author) ? "Anonymous" : author,
                    Content = content,
                    ScrapedAt = System.DateTime.UtcNow,
                    PostedAt = System.DateTime.TryParse(timeStr, out var postedAt) ? postedAt : System.DateTime.UtcNow
                };
                _db.ScrapedFeedbacks.Add(feedback);
                importedCount++;
            }
            await _db.SaveChangesAsync();
        }

        importFile.TotalRows = importedCount;
        importFile.ImportedRows = importedCount;
        importFile.Status = "completed";
        await _db.SaveChangesAsync();

        // Trigger AI analysis for the newly imported feedbacks
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();
                await aiService.AnalyzePendingFeedbacksAsync(projectId, false);
            }
            catch { }
        });

        var u = await _db.Users.FindAsync(userId);
        return new ImportFileDto
        {
            FileId = importFile.FileId,
            SourceId = importFile.SourceId,
            FileName = importFile.FileName,
            FileUrl = importFile.FileUrl,
            TotalRows = importFile.TotalRows,
            ImportedRows = importFile.ImportedRows,
            Status = importFile.Status,
            ImportedAt = importFile.ImportedAt,
            UploadedByName = u?.FullName ?? "Unknown"
        };
    }
    public async Task<bool> DeleteImportFileAsync(int workspaceId, int projectId, int fileId, int userId)
    {
        if (!await HasAccessAsync(workspaceId, projectId, userId))
            return false;

        var importFile = await _db.ImportFiles.FirstOrDefaultAsync(f => f.ProjectId == projectId && f.FileId == fileId);
        if (importFile == null)
            return false;

        var sql = @"
            DELETE FROM FEEDBACK_ASPECTS WHERE analysis_id IN (SELECT analysis_id FROM AI_ANALYSIS WHERE feedback_id IN (SELECT feedback_id FROM SCRAPED_FEEDBACKS WHERE import_file_id = {0}));
            DELETE FROM AI_ANALYSIS WHERE feedback_id IN (SELECT feedback_id FROM SCRAPED_FEEDBACKS WHERE import_file_id = {0});
            DELETE FROM MENTION_TAGS WHERE feedback_id IN (SELECT feedback_id FROM SCRAPED_FEEDBACKS WHERE import_file_id = {0});
            DELETE FROM SCRAPED_FEEDBACKS WHERE import_file_id = {0};
        ";
        await _db.Database.ExecuteSqlRawAsync(sql, fileId);

        _db.ImportFiles.Remove(importFile);
        await _db.SaveChangesAsync();
        return true;
    }
}
