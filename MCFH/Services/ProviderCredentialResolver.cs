using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// Resolver cho Brevo / PayOS credentials. Ưu tiên row IsDefault active trong DB,
/// fallback appsettings nếu DB trống (giữ behavior cũ cho local dev).
///
/// Cache 30 giây để tránh query DB mỗi email. Khi admin cập nhật key thì bị delay ≤ 30s —
/// chấp nhận được vì rotate key là thao tác hiếm.
/// </summary>
public interface IProviderCredentialResolver
{
    Task<ResolvedBrevo?> ResolveBrevoDefaultAsync(CancellationToken ct = default);
    Task<ResolvedPayOs?> ResolvePayOsDefaultAsync(CancellationToken ct = default);
    void Invalidate();
}

public record ResolvedBrevo(
    string KeyType,           // "api" | "smtp"
    string ApiKey,            // plain text (đã decrypt)
    string? SmtpLogin,        // chỉ cho smtp
    string FromAddress,
    string FromName,
    string? SmtpHost,         // host cấu hình
    int? SmtpPort             // port cấu hình
);

public record ResolvedPayOs(
    string ClientId,
    string ApiKey,
    string ChecksumKey
);

public class ProviderCredentialResolver : IProviderCredentialResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ProviderCredentialResolver> _logger;

    private DateTime _brevoCachedAt = DateTime.MinValue;
    private DateTime _payOsCachedAt = DateTime.MinValue;
    private ResolvedBrevo? _brevoCache;
    private ResolvedPayOs? _payOsCache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _brevoLock = new(1, 1);
    private readonly SemaphoreSlim _payOsLock = new(1, 1);

    public ProviderCredentialResolver(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ProviderCredentialResolver> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public void Invalidate()
    {
        _brevoCachedAt = DateTime.MinValue;
        _payOsCachedAt = DateTime.MinValue;
    }

    public async Task<ResolvedBrevo?> ResolveBrevoDefaultAsync(CancellationToken ct = default)
    {
        if (_brevoCache != null && DateTime.UtcNow - _brevoCachedAt < _cacheTtl)
            return _brevoCache;

        await _brevoLock.WaitAsync(ct);
        try
        {
            if (_brevoCache != null && DateTime.UtcNow - _brevoCachedAt < _cacheTtl)
                return _brevoCache;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Models.McfhDbContext>();
            var encryption = scope.ServiceProvider.GetRequiredService<EncryptionService>();

            var row = await db.BrevoKeys
                .AsNoTracking()
                .Where(b => b.IsDefault && b.Status == "active")
                .OrderByDescending(b => b.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (row == null)
            {
                // Fallback appsettings
                var fromAddress = _config["Smtp:FromAddress"];
                var fromName = _config["Smtp:FromName"] ?? "MCFH System Hub";
                var apiKey = _config["Smtp:ApiKey"];
                if (string.IsNullOrWhiteSpace(fromAddress) || fromAddress.StartsWith("REPLACE_"))
                    return null;

            if (!string.IsNullOrWhiteSpace(apiKey)
                && !apiKey.StartsWith("REPLACE_")
                && !apiKey.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase))
            {
                _brevoCache = new ResolvedBrevo("api", apiKey, null, fromAddress, fromName, null, null);
            }
            else
            {
                var smtpLogin = _config["Smtp:Username"];
                var smtpPass = _config["Smtp:Password"];
                if (string.IsNullOrWhiteSpace(smtpLogin) || string.IsNullOrWhiteSpace(smtpPass))
                    return null;
                var smtpPort = int.TryParse(_config["Smtp:Port"] ?? "587", out var port) ? port : 587;
                _brevoCache = new ResolvedBrevo(
                    "smtp",
                    smtpPass,
                    smtpLogin,
                    fromAddress,
                    fromName,
                    _config["Smtp:Host"] ?? "smtp-relay.brevo.com",
                    smtpPort);
            }
            _brevoCachedAt = DateTime.UtcNow;
            return _brevoCache;
            }

            var key = encryption.Decrypt(row.ApiKeyEncrypted) ?? "";
            if (string.IsNullOrWhiteSpace(key))
                return null;

            _brevoCache = new ResolvedBrevo(
                row.KeyType,
                key,
                row.SmtpLogin,
                row.FromAddress ?? _config["Smtp:FromAddress"] ?? "",
                row.FromName ?? _config["Smtp:FromName"] ?? "MCFH System Hub",
                _config["Smtp:Host"] ?? "smtp-relay.brevo.com",
                int.TryParse(_config["Smtp:Port"] ?? "587", out var p) ? p : 587);
            _brevoCachedAt = DateTime.UtcNow;

            // Background touch LastUsedAt
            _ = Task.Run(async () =>
            {
                try
                {
                    using var s = _scopeFactory.CreateScope();
                    var d = s.ServiceProvider.GetRequiredService<Models.McfhDbContext>();
                    var r = await d.BrevoKeys.FirstOrDefaultAsync(x => x.BrevoKeyId == row.BrevoKeyId);
                    if (r != null)
                    {
                        r.LastUsedAt = DateTime.Now;
                        await d.SaveChangesAsync();
                    }
                }
                catch { /* best effort */ }
            });

            return _brevoCache;
        }
        finally
        {
            _brevoLock.Release();
        }
    }

    public async Task<ResolvedPayOs?> ResolvePayOsDefaultAsync(CancellationToken ct = default)
    {
        if (_payOsCache != null && DateTime.UtcNow - _payOsCachedAt < _cacheTtl)
            return _payOsCache;

        await _payOsLock.WaitAsync(ct);
        try
        {
            if (_payOsCache != null && DateTime.UtcNow - _payOsCachedAt < _cacheTtl)
                return _payOsCache;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Models.McfhDbContext>();
            var encryption = scope.ServiceProvider.GetRequiredService<EncryptionService>();

            var row = await db.PayOsKeys
                .AsNoTracking()
                .Where(p => p.IsDefault && p.Status == "active")
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (row == null)
            {
                // Fallback appsettings — PayOsOptions binding
                var clientId = _config["PayOS:ClientId"];
                var apiKey = _config["PayOS:ApiKey"];
                var checksum = _config["PayOS:ChecksumKey"];
                if (string.IsNullOrWhiteSpace(clientId)
                    || string.IsNullOrWhiteSpace(apiKey)
                    || string.IsNullOrWhiteSpace(checksum)
                    || clientId.StartsWith("REPLACE_")
                    || apiKey.StartsWith("REPLACE_")
                    || checksum.StartsWith("REPLACE_"))
                {
                    return null;
                }

                _payOsCache = new ResolvedPayOs(clientId, apiKey, checksum);
                _payOsCachedAt = DateTime.UtcNow;
                return _payOsCache;
            }

            var apiKey2 = encryption.Decrypt(row.ApiKeyEncrypted) ?? "";
            var checksum2 = encryption.Decrypt(row.ChecksumKeyEncrypted) ?? "";
            if (string.IsNullOrWhiteSpace(apiKey2) || string.IsNullOrWhiteSpace(checksum2))
                return null;

            _payOsCache = new ResolvedPayOs(row.ClientId, apiKey2, checksum2);
            _payOsCachedAt = DateTime.UtcNow;
            return _payOsCache;
        }
        finally
        {
            _payOsLock.Release();
        }
    }
}
