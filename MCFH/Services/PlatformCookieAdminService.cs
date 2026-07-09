using MCFH.DTOs;
using MCFH.Models;
using MCFH.Models.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class PlatformCookieAdminService
{
    private readonly McfhDbContext _context;
    private readonly IPlatformCookiePathProvider _pathProvider;

    public PlatformCookieAdminService(McfhDbContext context, IPlatformCookiePathProvider pathProvider)
    {
        _context = context;
        _pathProvider = pathProvider;
    }

    public async Task<List<PlatformCookieDto>> ListAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId))
            return new();

        var rows = await _context.PlatformCookies
            .OrderBy(p => p.Platform)
            .ToListAsync();

        var result = new List<PlatformCookieDto>();
        foreach (var row in rows)
            result.Add(await MapDtoAsync(row, includeRequired: false));

        return result;
    }

    public async Task<PlatformCookieDto?> GetAsync(int adminUserId, string platform)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        platform = PlatformCookieFileHelper.NormalizePlatform(platform);
        var row = await _context.PlatformCookies.FirstOrDefaultAsync(p => p.Platform == platform);
        return row == null ? null : await MapDtoAsync(row, includeRequired: true);
    }

    public async Task<PlatformCookieDto?> UpdateMetaAsync(
        int adminUserId,
        string platform,
        UpdatePlatformCookieMetaDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        platform = PlatformCookieFileHelper.NormalizePlatform(platform);
        var row = await _context.PlatformCookies.FirstOrDefaultAsync(p => p.Platform == platform);
        if (row == null)
            return null;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = dto.Status.Trim().ToLowerInvariant();
            if (status is not ("active" or "disabled" or "expired"))
                throw new ArgumentException("status phải là active, disabled hoặc expired.");
            row.Status = status;
        }

        if (dto.Note != null)
            row.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        if (!string.IsNullOrWhiteSpace(dto.FilePath))
        {
            var relative = dto.FilePath.Replace('\\', '/').Trim();
            if (!_pathProvider.IsRelativePathAllowed(relative))
                throw new ArgumentException("file_path phải nằm trong thư mục cookies/.");
            row.FilePath = relative;
        }

        await _context.SaveChangesAsync();
        _pathProvider.InvalidateCache();
        return await MapDtoAsync(row, includeRequired: true);
    }

    public async Task<PlatformCookieContentResultDto?> UpdateContentAsync(
        int adminUserId,
        string platform,
        UpdatePlatformCookieContentDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            return null;

        platform = PlatformCookieFileHelper.NormalizePlatform(platform);
        var row = await _context.PlatformCookies.FirstOrDefaultAsync(p => p.Platform == platform);
        if (row == null)
            return null;

        var entries = PlatformCookieFileHelper.ParseCookies(dto);
        PlatformCookieFileHelper.ValidateRequiredCookies(platform, entries);

        if (!_pathProvider.IsRelativePathAllowed(row.FilePath))
            throw new ArgumentException("file_path trong DB không hợp lệ.");

        var fullPath = _pathProvider.ToFullPath(row.FilePath);
        var backupRelative = _pathProvider.GetBackupRelativePath(platform, row.FilePath);
        var backupFull = _pathProvider.ToFullPath(backupRelative);
        var backupCreated = false;

        if (File.Exists(fullPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupFull)!);
            File.Copy(fullPath, backupFull, overwrite: true);
            backupCreated = true;
        }

        await PlatformCookieFileHelper.WriteCookieFileAsync(fullPath, entries);

        var now = DateTime.Now;
        row.CookieCount = entries.Count;
        row.ExpiresAt = PlatformCookieFileHelper.ComputeExpiresAt(entries);
        row.UploadedAt = now;
        row.Status = "active";

        await _context.SaveChangesAsync();
        _pathProvider.InvalidateCache();

        return new PlatformCookieContentResultDto
        {
            Message = $"Đã cập nhật cookie {platform}.",
            Platform = platform,
            FilePath = row.FilePath,
            CookieCount = row.CookieCount,
            ExpiresAt = row.ExpiresAt,
            UploadedAt = row.UploadedAt,
            BackupCreated = backupCreated
        };
    }

    public async Task<bool> ClearContentAsync(int adminUserId, string platform)
    {
        if (!await IsAdminAsync(adminUserId))
            return false;

        platform = PlatformCookieFileHelper.NormalizePlatform(platform);
        var row = await _context.PlatformCookies.FirstOrDefaultAsync(p => p.Platform == platform);
        if (row == null)
            return false;

        if (_pathProvider.IsRelativePathAllowed(row.FilePath))
        {
            var fullPath = _pathProvider.ToFullPath(row.FilePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        row.CookieCount = 0;
        row.ExpiresAt = null;
        row.Status = "disabled";
        await _context.SaveChangesAsync();
        _pathProvider.InvalidateCache();
        return true;
    }

    private async Task<PlatformCookieDto> MapDtoAsync(PlatformCookie row, bool includeRequired)
    {
        var fullPath = _pathProvider.IsRelativePathAllowed(row.FilePath)
            ? _pathProvider.ToFullPath(row.FilePath)
            : null;
        var fileExists = fullPath != null && File.Exists(fullPath);

        var backupRelative = _pathProvider.GetBackupRelativePath(row.Platform, row.FilePath);
        var backupFull = _pathProvider.ToFullPath(backupRelative);

        List<CookieEditorEntry>? entries = null;
        if (includeRequired && fileExists && fullPath != null)
            entries = await PlatformCookieFileHelper.TryReadCookieFileAsync(fullPath);

        return new PlatformCookieDto
        {
            PlatformCookieId = row.PlatformCookieId,
            Platform = row.Platform,
            FilePath = row.FilePath,
            Status = row.Status,
            Note = row.Note,
            CookieCount = fileExists ? (entries?.Count ?? row.CookieCount) : row.CookieCount,
            ExpiresAt = row.ExpiresAt,
            UploadedAt = row.UploadedAt,
            LastUsedAt = row.LastUsedAt,
            FileExists = fileExists,
            FileMissing = !fileExists,
            IsExpiringSoon = PlatformCookieFileHelper.IsExpiringSoon(row.ExpiresAt),
            BackupFilePath = backupRelative,
            BackupExists = File.Exists(backupFull),
            RequiredCookiesPresent = includeRequired
                ? PlatformCookieFileHelper.GetRequiredPresence(row.Platform, entries)
                : null
        };
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
