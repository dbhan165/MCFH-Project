using MCFH.DTOs;
using MCFH.Models;
using MCFH.Models.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class PlatformCookieAdminService
{
    private readonly McfhDbContext _context;
    private readonly IPlatformCookiePathProvider _pathProvider;
    private readonly ILogger<PlatformCookieAdminService> _logger;

    public PlatformCookieAdminService(
        McfhDbContext context,
        IPlatformCookiePathProvider pathProvider,
        ILogger<PlatformCookieAdminService> logger)
    {
        _context = context;
        _pathProvider = pathProvider;
        _logger = logger;
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
        {
            try
            {
                result.Add(await MapDtoAsync(row, includeRequired: false));
            }
            catch (Exception ex)
            {
                // Không để 1 row hỏng làm hỏng cả list. Log chi tiết để debug.
                _logger.LogError(ex,
                    "MapDtoAsync thất bại cho PLATFORM_COOKIES id={Id} platform={Platform} filePath={FilePath}",
                    row.PlatformCookieId, row.Platform, row.FilePath);
                // Trả row tối thiểu để UI vẫn hiển thị thay vì trắng.
                result.Add(new PlatformCookieDto
                {
                    PlatformCookieId = row.PlatformCookieId,
                    Platform = row.Platform,
                    FilePath = row.FilePath,
                    Status = ComputeEffectiveStatus(row),
                    Note = row.Note,
                    CookieCount = row.CookieCount,
                    ExpiresAt = row.ExpiresAt,
                    UploadedAt = row.UploadedAt,
                    LastUsedAt = row.LastUsedAt,
                    FileExists = false,
                    FileMissing = true,
                    IsExpiringSoon = PlatformCookieFileHelper.IsExpiringSoon(row.ExpiresAt),
                    IsExpired = PlatformCookieFileHelper.IsExpired(row.ExpiresAt),
                    RequiredCookiesPresent = null
                });
            }
        }

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

        // Đổi tên file JSON trên disk nếu file_path thay đổi.
        // - Source path phải hợp lệ (đã được lưu trước đó).
        // - Target path phải nằm trong cookies/, chưa tồn tại trên disk.
        // - Backup file (nếu có) được đổi tên tương ứng.
        if (!string.IsNullOrWhiteSpace(dto.FilePath))
        {
            var relative = dto.FilePath.Replace('\\', '/').Trim();
            if (!_pathProvider.IsRelativePathAllowed(relative))
                throw new ArgumentException("file_path phải nằm trong thư mục cookies/.");

            if (!string.Equals(relative, row.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                var oldFullPath = _pathProvider.IsRelativePathAllowed(row.FilePath)
                    ? _pathProvider.ToFullPath(row.FilePath)
                    : null;
                var newFullPath = _pathProvider.ToFullPath(relative);

                if (oldFullPath != null && File.Exists(oldFullPath))
                {
                    if (File.Exists(newFullPath))
                        throw new ArgumentException(
                            $"File mới '{relative}' đã tồn tại trên disk. Hãy đổi tên khác hoặc xóa file cũ trước.");

                    var oldDir = Path.GetDirectoryName(oldFullPath);
                    var newDir = Path.GetDirectoryName(newFullPath);
                    if (!string.IsNullOrEmpty(newDir) && newDir != oldDir)
                        Directory.CreateDirectory(newDir);

                    File.Move(oldFullPath, newFullPath);

                    // Backup cũ (nếu có) cũng theo tên mới để không mất lịch sử.
                    var oldBackup = _pathProvider.ToFullPath(
                        _pathProvider.GetBackupRelativePath(platform, row.FilePath));
                    var newBackup = _pathProvider.ToFullPath(
                        _pathProvider.GetBackupRelativePath(platform, relative));
                    if (File.Exists(oldBackup) && !File.Exists(newBackup))
                    {
                        var newBackupDir = Path.GetDirectoryName(newBackup);
                        if (!string.IsNullOrEmpty(newBackupDir))
                            Directory.CreateDirectory(newBackupDir);
                        File.Move(oldBackup, newBackup);
                    }
                }

                row.FilePath = relative;
            }
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

    public async Task<PlatformCookieDto> CreateAsync(int adminUserId, CreatePlatformCookieDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            throw new UnauthorizedAccessException("Chỉ Admin được tạo platform cookie.");

        // Validate platform key: lowercase letters/digits/underscore, length 2..50.
        var platform = (dto.Platform ?? string.Empty).Trim().ToLowerInvariant();
        if (platform.Length is < 2 or > 50 || !platform.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))
            throw new ArgumentException("platform phải là chữ thường/chữ số/dấu gạch, dài 2-50 ký tự.");

        if (await _context.PlatformCookies.AnyAsync(p => p.Platform == platform))
            throw new ArgumentException($"Platform '{platform}' đã tồn tại.");

        // Validate file_path tương đối.
        var filePath = (dto.FilePath ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("file_path là bắt buộc.");
        if (!_pathProvider.IsRelativePathAllowed(filePath))
            throw new ArgumentException("file_path phải nằm trong thư mục cookies/.");

        var status = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(status))
            status = "disabled";
        if (status is not ("active" or "disabled" or "expired"))
            throw new ArgumentException("status phải là active, disabled hoặc expired.");

        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        // Tạo record với status=disabled trước; nếu có cookie hợp lệ thì cập nhật sau.
        var now = DateTime.Now;
        var row = new PlatformCookie
        {
            Platform = platform,
            FilePath = filePath,
            Status = status,
            Note = note,
            CookieCount = 0,
            CreatedAt = now
        };

        _context.PlatformCookies.Add(row);
        await _context.SaveChangesAsync();

        // Nếu có CookiesJson thì parse + ghi file + cập nhật expires/count.
        if (!string.IsNullOrWhiteSpace(dto.CookiesJson))
        {
            var entries = PlatformCookieFileHelper.ParseCookies(new UpdatePlatformCookieContentDto
            {
                CookiesJson = dto.CookiesJson
            });
            PlatformCookieFileHelper.ValidateRequiredCookies(platform, entries);

            var fullPath = _pathProvider.ToFullPath(filePath);
            await PlatformCookieFileHelper.WriteCookieFileAsync(fullPath, entries);

            row.CookieCount = entries.Count;
            row.ExpiresAt = PlatformCookieFileHelper.ComputeExpiresAt(entries);
            row.UploadedAt = now;
            row.Status = "active";
            await _context.SaveChangesAsync();
        }

        _pathProvider.InvalidateCache();
        _logger.LogInformation(
            "Admin {UserId} tạo PLATFORM_COOKIES platform={Platform} filePath={FilePath} status={Status}",
            adminUserId, platform, filePath, row.Status);

        return await MapDtoAsync(row, includeRequired: true);
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
            Status = ComputeEffectiveStatus(row),
            Note = row.Note,
            CookieCount = fileExists ? (entries?.Count ?? row.CookieCount) : row.CookieCount,
            ExpiresAt = row.ExpiresAt,
            UploadedAt = row.UploadedAt,
            LastUsedAt = row.LastUsedAt,
            FileExists = fileExists,
            FileMissing = !fileExists,
            IsExpiringSoon = PlatformCookieFileHelper.IsExpiringSoon(row.ExpiresAt),
            IsExpired = PlatformCookieFileHelper.IsExpired(row.ExpiresAt),
            BackupFilePath = backupRelative,
            BackupExists = File.Exists(backupFull),
            RequiredCookiesPresent = includeRequired
                ? PlatformCookieFileHelper.GetRequiredPresence(row.Platform, entries)
                : null
        };
    }

    private static string ComputeEffectiveStatus(PlatformCookie row)
    {
        // Nếu status thủ công là disabled thì giữ nguyên.
        var manual = (row.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (manual == "disabled")
            return "disabled";

        // Đã hết hạn thật sự (expiresAt < now) → hiển thị expired dù DB đang active.
        if (row.ExpiresAt.HasValue && row.ExpiresAt.Value < DateTime.Now)
            return "expired";

        return string.IsNullOrWhiteSpace(manual) ? "active" : manual;
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
