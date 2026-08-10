using MCFH.DTOs.Admin.ProviderKeys;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// CRUD cho BrevoKey. Multi-secret vault: nhiều row, chỉ 1 row có IsDefault=true active.
/// EmailService dùng row default khi startup/runtime; admin rotate key qua create + flag default.
/// </summary>
public class ProviderKeyAdminService
{
    private readonly McfhDbContext _context;
    private readonly EncryptionService _encryption;
    private readonly IProviderCredentialResolver _resolver;
    private readonly ILogger<ProviderKeyAdminService> _logger;

    public ProviderKeyAdminService(
        McfhDbContext context,
        EncryptionService encryption,
        IProviderCredentialResolver resolver,
        ILogger<ProviderKeyAdminService> logger)
    {
        _context = context;
        _encryption = encryption;
        _resolver = resolver;
        _logger = logger;
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        if (userId <= 0) return false;
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sau khi thay đổi key default, invalidate cache để services đọc lại.</summary>
    private void InvalidateResolverCaches()
    {
        try { _resolver.Invalidate(); }
        catch { /* best effort */ }
    }

    private async Task SaveAndInvalidateAsync()
    {
        await _context.SaveChangesAsync();
        InvalidateResolverCaches();
    }

    // ===== BREVO =====

    public async Task<List<BrevoKeyDto>> ListBrevoAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();
        var rows = await _context.BrevoKeys
            .AsNoTracking()
            .OrderByDescending(b => b.IsDefault)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        return rows.Select(MapBrevo).ToList();
    }

    public async Task<BrevoKeyDto?> GetBrevoAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.BrevoKeys.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BrevoKeyId == id);
        return row == null ? null : MapBrevo(row);
    }

    /// <summary>Trả về full key (chỉ dùng khi admin chủ động reveal).</summary>
    public async Task<BrevoKeyRevealDto?> RevealBrevoAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.BrevoKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BrevoKeyId == id);
        if (row == null) return null;

        return new BrevoKeyRevealDto
        {
            BrevoKeyId = row.BrevoKeyId,
            ApiKey = _encryption.Decrypt(row.ApiKeyEncrypted) ?? "",
            SmtpLogin = row.SmtpLogin
        };
    }

    public async Task<BrevoKeyDto> CreateBrevoAsync(int adminUserId, CreateBrevoKeyDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            throw new UnauthorizedAccessException("Không có quyền Admin.");
        var keyType = (dto.KeyType ?? "api").Trim().ToLowerInvariant();
        if (keyType is not ("api" or "smtp"))
            throw new InvalidOperationException("KeyType chỉ chấp nhận 'api' hoặc 'smtp'.");

        if (string.IsNullOrWhiteSpace(dto.ApiKey))
            throw new InvalidOperationException("ApiKey là bắt buộc.");

        var row = new BrevoKey
        {
            KeyType = keyType,
            ApiKeyEncrypted = _encryption.Encrypt(dto.ApiKey) ?? "",
            SmtpLogin = dto.SmtpLogin?.Trim(),
            FromAddress = dto.FromAddress?.Trim(),
            FromName = dto.FromName?.Trim(),
            Status = "active",
            IsDefault = dto.IsDefault,
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UpdatedBy = adminUserId
        };

        if (dto.IsDefault) await ClearOtherBrevoDefaultAsync(null);

        _context.BrevoKeys.Add(row);
        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} tạo BrevoKey id={Id} type={Type} isDefault={Default}",
            adminUserId, row.BrevoKeyId, row.KeyType, row.IsDefault);

        return MapBrevo(row);
    }

    public async Task<BrevoKeyDto?> UpdateBrevoAsync(int adminUserId, int id, UpdateBrevoKeyDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.BrevoKeys.FirstOrDefaultAsync(b => b.BrevoKeyId == id);
        if (row == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            row.ApiKeyEncrypted = _encryption.Encrypt(dto.ApiKey) ?? row.ApiKeyEncrypted;

        if (dto.SmtpLogin != null) row.SmtpLogin = dto.SmtpLogin.Trim();
        if (dto.FromAddress != null) row.FromAddress = dto.FromAddress.Trim();
        if (dto.FromName != null) row.FromName = dto.FromName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var s = dto.Status.Trim().ToLowerInvariant();
            if (s is not ("active" or "disabled"))
                throw new InvalidOperationException("Status không hợp lệ.");
            row.Status = s;
            if (s == "disabled" && row.IsDefault)
            {
                // Không cho default khi disabled
                row.IsDefault = false;
            }
        }

        if (dto.IsDefault.HasValue)
        {
            if (dto.IsDefault.Value && row.Status == "active")
            {
                await ClearOtherBrevoDefaultAsync(id);
                row.IsDefault = true;
            }
            else
            {
                row.IsDefault = false;
            }
        }

        if (dto.Note != null) row.Note = dto.Note.Trim();

        row.UpdatedAt = DateTime.Now;
        row.UpdatedBy = adminUserId;

        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} cập nhật BrevoKey id={Id}", adminUserId, id);
        return MapBrevo(row);
    }

    public async Task<bool> DeleteBrevoAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return false;
        var row = await _context.BrevoKeys.FirstOrDefaultAsync(b => b.BrevoKeyId == id);
        if (row == null) return false;
        if (row.IsDefault)
            throw new InvalidOperationException("Không thể xóa key đang được set Default. Hãy set default sang key khác trước.");

        _context.BrevoKeys.Remove(row);
        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} xóa BrevoKey id={Id}", adminUserId, id);
        return true;
    }

    private async Task ClearOtherBrevoDefaultAsync(int? keepId)
    {
        var others = await _context.BrevoKeys
            .Where(b => b.IsDefault && b.BrevoKeyId != keepId)
            .ToListAsync();
        foreach (var o in others) o.IsDefault = false;
    }

    private static BrevoKeyDto MapBrevo(BrevoKey row) => new()
    {
        BrevoKeyId = row.BrevoKeyId,
        KeyType = row.KeyType,
        SmtpLogin = row.SmtpLogin,
        FromAddress = row.FromAddress,
        FromName = row.FromName,
        Status = row.Status,
        IsDefault = row.IsDefault,
        ApiKeyMasked = SecretKeyMasker.MaskPlaintext(row.ApiKeyEncrypted, EncryptionService.StaticDecrypt),
        Note = row.Note,
        LastUsedAt = row.LastUsedAt,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        UpdatedBy = row.UpdatedBy
    };

    // ===== PAYOS =====

    public async Task<List<PayOsKeyDto>> ListPayOsAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();
        var rows = await _context.PayOsKeys
            .AsNoTracking()
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return rows.Select(MapPayOs).ToList();
    }

    public async Task<PayOsKeyDto?> GetPayOsAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.PayOsKeys.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PayOsKeyId == id);
        return row == null ? null : MapPayOs(row);
    }

    public async Task<PayOsKeyRevealDto?> RevealPayOsAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.PayOsKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PayOsKeyId == id);
        if (row == null) return null;

        return new PayOsKeyRevealDto
        {
            PayOsKeyId = row.PayOsKeyId,
            ClientId = row.ClientId,
            ApiKey = _encryption.Decrypt(row.ApiKeyEncrypted) ?? "",
            ChecksumKey = _encryption.Decrypt(row.ChecksumKeyEncrypted) ?? ""
        };
    }

    public async Task<PayOsKeyDto> CreatePayOsAsync(int adminUserId, CreatePayOsKeyDto dto)
    {
        if (!await IsAdminAsync(adminUserId))
            throw new UnauthorizedAccessException("Không có quyền Admin.");
        if (string.IsNullOrWhiteSpace(dto.ClientId))
            throw new InvalidOperationException("ClientId là bắt buộc.");
        if (string.IsNullOrWhiteSpace(dto.ApiKey))
            throw new InvalidOperationException("ApiKey là bắt buộc.");
        if (string.IsNullOrWhiteSpace(dto.ChecksumKey))
            throw new InvalidOperationException("ChecksumKey là bắt buộc.");

        var env = (dto.Environment ?? "live").Trim().ToLowerInvariant();
        if (env is not ("sandbox" or "live"))
            throw new InvalidOperationException("Environment chỉ chấp nhận 'sandbox' hoặc 'live'.");

        var row = new PayOsKey
        {
            ClientId = dto.ClientId.Trim(),
            ApiKeyEncrypted = _encryption.Encrypt(dto.ApiKey) ?? "",
            ChecksumKeyEncrypted = _encryption.Encrypt(dto.ChecksumKey) ?? "",
            Environment = env,
            Status = "active",
            IsDefault = dto.IsDefault,
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UpdatedBy = adminUserId
        };

        if (dto.IsDefault) await ClearOtherPayOsDefaultAsync(null);

        _context.PayOsKeys.Add(row);
        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} tạo PayOsKey id={Id} env={Env} isDefault={Default}",
            adminUserId, row.PayOsKeyId, row.Environment, row.IsDefault);

        return MapPayOs(row);
    }

    public async Task<PayOsKeyDto?> UpdatePayOsAsync(int adminUserId, int id, UpdatePayOsKeyDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;
        var row = await _context.PayOsKeys.FirstOrDefaultAsync(p => p.PayOsKeyId == id);
        if (row == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.ClientId))
            row.ClientId = dto.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            row.ApiKeyEncrypted = _encryption.Encrypt(dto.ApiKey) ?? row.ApiKeyEncrypted;
        if (!string.IsNullOrWhiteSpace(dto.ChecksumKey))
            row.ChecksumKeyEncrypted = _encryption.Encrypt(dto.ChecksumKey) ?? row.ChecksumKeyEncrypted;

        if (!string.IsNullOrWhiteSpace(dto.Environment))
        {
            var env = dto.Environment.Trim().ToLowerInvariant();
            if (env is not ("sandbox" or "live"))
                throw new InvalidOperationException("Environment không hợp lệ.");
            row.Environment = env;
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var s = dto.Status.Trim().ToLowerInvariant();
            if (s is not ("active" or "disabled"))
                throw new InvalidOperationException("Status không hợp lệ.");
            row.Status = s;
            if (s == "disabled" && row.IsDefault)
                row.IsDefault = false;
        }

        if (dto.IsDefault.HasValue)
        {
            if (dto.IsDefault.Value && row.Status == "active")
            {
                await ClearOtherPayOsDefaultAsync(id);
                row.IsDefault = true;
            }
            else
            {
                row.IsDefault = false;
            }
        }

        if (dto.Note != null) row.Note = dto.Note.Trim();

        row.UpdatedAt = DateTime.Now;
        row.UpdatedBy = adminUserId;

        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} cập nhật PayOsKey id={Id}", adminUserId, id);
        return MapPayOs(row);
    }

    public async Task<bool> DeletePayOsAsync(int adminUserId, int id)
    {
        if (!await IsAdminAsync(adminUserId)) return false;
        var row = await _context.PayOsKeys.FirstOrDefaultAsync(p => p.PayOsKeyId == id);
        if (row == null) return false;
        if (row.IsDefault)
            throw new InvalidOperationException("Không thể xóa key đang được set Default. Hãy set default sang key khác trước.");

        _context.PayOsKeys.Remove(row);
        await SaveAndInvalidateAsync();

        _logger.LogInformation("Admin {UserId} xóa PayOsKey id={Id}", adminUserId, id);
        return true;
    }

    private async Task ClearOtherPayOsDefaultAsync(int? keepId)
    {
        var others = await _context.PayOsKeys
            .Where(p => p.IsDefault && p.PayOsKeyId != keepId)
            .ToListAsync();
        foreach (var o in others) o.IsDefault = false;
    }

    private static PayOsKeyDto MapPayOs(PayOsKey row) => new()
    {
        PayOsKeyId = row.PayOsKeyId,
        ClientId = row.ClientId,
        ApiKeyMasked = SecretKeyMasker.MaskPlaintext(row.ApiKeyEncrypted, EncryptionService.StaticDecrypt),
        ChecksumKeyMasked = SecretKeyMasker.MaskPlaintext(row.ChecksumKeyEncrypted, EncryptionService.StaticDecrypt),
        Environment = row.Environment,
        Status = row.Status,
        IsDefault = row.IsDefault,
        Note = row.Note,
        LastUsedAt = row.LastUsedAt,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        UpdatedBy = row.UpdatedBy
    };

    // =====================================================================
    // Seed helpers — chuyển giá trị từ appsettings.json vào DB một lần.
    // Admin chủ động gọi (qua CLI/utility endpoint) sau khi cấu hình prod.
    // KHÔNG có plain text bị persist ngoài DB đã encrypt.
    // =====================================================================

    /// <summary>Seed 1 Brevo key từ raw values (lấy từ appsettings.json).</summary>
    public async Task<BrevoKeyDto> SeedBrevoFromConfigAsync(int adminUserId,
        string keyType,
        string apiKeyPlain,
        string? smtpLogin,
        string? fromAddress,
        string? fromName)
    {
        // Nếu đã có row default → bỏ qua
        var existing = await _context.BrevoKeys.FirstOrDefaultAsync(b => b.IsDefault);
        if (existing != null)
        {
            _logger.LogInformation("BREVO_KEYS đã có default id={Id}, skip seed", existing.BrevoKeyId);
            return MapBrevo(existing);
        }

        var row = new BrevoKey
        {
            KeyType = keyType,
            ApiKeyEncrypted = _encryption.Encrypt(apiKeyPlain) ?? "",
            SmtpLogin = smtpLogin,
            FromAddress = fromAddress,
            FromName = fromName,
            Status = "active",
            IsDefault = true,
            Note = "Seeded từ appsettings.json — review và update nếu cần.",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UpdatedBy = adminUserId
        };
        _context.BrevoKeys.Add(row);
        await SaveAndInvalidateAsync();
        _logger.LogInformation("Seeded BrevoKey id={Id} type={Type}", row.BrevoKeyId, row.KeyType);
        return MapBrevo(row);
    }

    /// <summary>Seed 1 PayOS key từ raw values (lấy từ appsettings.json).</summary>
    public async Task<PayOsKeyDto> SeedPayOsFromConfigAsync(int adminUserId,
        string clientId,
        string apiKeyPlain,
        string checksumKeyPlain,
        string environment)
    {
        var existing = await _context.PayOsKeys.FirstOrDefaultAsync(p => p.IsDefault);
        if (existing != null)
        {
            _logger.LogInformation("PAYOS_KEYS đã có default id={Id}, skip seed", existing.PayOsKeyId);
            return MapPayOs(existing);
        }

        var row = new PayOsKey
        {
            ClientId = clientId,
            ApiKeyEncrypted = _encryption.Encrypt(apiKeyPlain) ?? "",
            ChecksumKeyEncrypted = _encryption.Encrypt(checksumKeyPlain) ?? "",
            Environment = environment,
            Status = "active",
            IsDefault = true,
            Note = "Seeded từ appsettings.json — review và update nếu cần.",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            UpdatedBy = adminUserId
        };
        _context.PayOsKeys.Add(row);
        await SaveAndInvalidateAsync();
        _logger.LogInformation("Seeded PayOsKey id={Id} env={Env}", row.PayOsKeyId, row.Environment);
        return MapPayOs(row);
    }
}
