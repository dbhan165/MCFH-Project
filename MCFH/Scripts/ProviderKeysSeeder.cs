using MCFH.Models;
using MCFH.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MCFH.Scripts;

/// <summary>
/// CLI helper: chạy một lần sau khi deploy để chuyển keys từ appsettings vào DB.
/// Cách dùng:
///   dotnet run --project MCFH -- --seed-provider-keys
/// hoặc từ Program.cs (xem entry point trong Program.cs).
///
/// Sau khi chạy thành công:
///   - Brevo email service tự dùng DB row
///   - PayOS service tự dùng DB row
///   - Bạn có thể XÓA key trong appsettings.json để tránh lộ
/// </summary>
public class ProviderKeysSeeder
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<ProviderKeysSeeder> _logger;

    public ProviderKeysSeeder(IServiceProvider sp, IConfiguration config, ILogger<ProviderKeysSeeder> logger)
    {
        _sp = sp;
        _config = config;
        _logger = logger;
    }

    public async Task<int> RunAsync()
    {
        var count = 0;

        using var scope = _sp.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<ProviderKeyAdminService>();
        const int SystemAdminId = 1; // seed bằng system admin (user_id=1) nếu có

        // ── BREVO ──
        var fromAddress = _config["Smtp:FromAddress"];
        var fromName = _config["Smtp:FromName"];
        var smtpApiKey = _config["Smtp:ApiKey"];
        var smtpUsername = _config["Smtp:Username"];
        var smtpPassword = _config["Smtp:Password"];

        if (!string.IsNullOrWhiteSpace(fromAddress)
            && !fromAddress.StartsWith("REPLACE_", StringComparison.Ordinal))
        {
            string keyType;
            string brevoApiKey;

            if (!string.IsNullOrWhiteSpace(smtpApiKey)
                && !smtpApiKey.StartsWith("REPLACE_", StringComparison.Ordinal)
                && !smtpApiKey.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase))
            {
                keyType = "api";
                brevoApiKey = smtpApiKey;
            }
            else if (!string.IsNullOrWhiteSpace(smtpUsername)
                     && !string.IsNullOrWhiteSpace(smtpPassword)
                     && !smtpUsername.StartsWith("REPLACE_")
                     && !smtpPassword.StartsWith("REPLACE_"))
            {
                keyType = "smtp";
                brevoApiKey = smtpPassword; // mã hóa password làm api_key
            }
            else
            {
                _logger.LogWarning("appsettings không có Smtp config hợp lệ. Bỏ qua seed Brevo.");
                keyType = null!;
                brevoApiKey = null!;
            }

            if (!string.IsNullOrEmpty(keyType))
            {
                try
                {
                    var seeded = await adminService.SeedBrevoFromConfigAsync(
                        SystemAdminId, keyType, brevoApiKey,
                        keyType == "smtp" ? smtpUsername : null,
                        fromAddress, fromName);
                    if (seeded.BrevoKeyId > 0)
                    {
                        _logger.LogInformation("✓ Seeded BrevoKey #{Id}", seeded.BrevoKeyId);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Seed Brevo thất bại");
                }
            }
        }
        else
        {
            _logger.LogInformation("appsettings.Smtp:FromAddress không có / là REPLACE_*. Bỏ qua Brevo.");
        }

        // ── PAYOS ──
        var clientId = _config["PayOS:ClientId"];
        var apiKey = _config["PayOS:ApiKey"];
        var checksumKey = _config["PayOS:ChecksumKey"];

        if (!string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(apiKey)
            && !string.IsNullOrWhiteSpace(checksumKey)
            && !clientId.StartsWith("REPLACE_")
            && !apiKey.StartsWith("REPLACE_")
            && !checksumKey.StartsWith("REPLACE_"))
        {
            var env = _config["PayOS:Bypass"]?.ToString() == "true" ? "sandbox" : "live";

            try
            {
                var seeded = await adminService.SeedPayOsFromConfigAsync(
                    SystemAdminId, clientId, apiKey, checksumKey, env);
                if (seeded.PayOsKeyId > 0)
                {
                    _logger.LogInformation("✓ Seeded PayOsKey #{Id} env={Env}",
                        seeded.PayOsKeyId, seeded.Environment);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seed PayOS thất bại");
            }
        }
        else
        {
            _logger.LogInformation("appsettings.PayOS:* chưa cấu hình hoặc là REPLACE_*. Bỏ qua PayOS.");
        }

        _logger.LogInformation("Seed hoàn tất: {Count} key(s) đã thêm.", count);
        return count;
    }
}
