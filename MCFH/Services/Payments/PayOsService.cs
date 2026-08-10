using MCFH.Configuration;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace MCFH.Services.Payments;

/// <summary>
/// Wrapper quanh SDK payOS (PayOSClient): tạo payment link, verify webhook, tra cứu trạng thái.
/// Client được khởi tạo lazy để app vẫn chạy được khi chưa cấu hình PayOS (chỉ fail khi dùng đến).
///
/// Ưu tiên row PayOS default trong DB (qua IProviderCredentialResolver),
/// fallback appsettings nếu DB không có row (giữ behavior cho dev).
/// </summary>
public class PayOsService
{
    private readonly PayOsOptions _options;
    private readonly AuthOptions _authOptions;
    private readonly IProviderCredentialResolver _resolver;
    private readonly ILogger<PayOsService> _logger;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private PayOSClient? _client;
    private string? _clientFingerprint;

    public PayOsService(
        IOptions<PayOsOptions> options,
        IOptions<AuthOptions> authOptions,
        IProviderCredentialResolver resolver,
        ILogger<PayOsService> logger)
    {
        _options = options.Value;
        _authOptions = authOptions.Value;
        _resolver = resolver;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    private async Task<PayOSClient> GetClientAsync()
    {
        // Resolve credentials mỗi lần gọi (đã có cache 30s ở resolver).
        var dbCred = await _resolver.ResolvePayOsDefaultAsync();

        string fingerprint;

        if (dbCred != null)
        {
            fingerprint = $"db:{dbCred.ClientId}|{dbCred.ApiKey}|{dbCred.ChecksumKey}";
        }
        else
        {
            // Fallback appsettings
            if (!_options.IsConfigured)
            {
                throw new InvalidOperationException(
                    "PayOS chưa được cấu hình. Thêm PayOS key default qua Admin → Cài đặt hệ thống → PayOS Key, " +
                    "hoặc thiết lập PayOS:ClientId/ApiKey/ChecksumKey trong appsettings.");
            }
            fingerprint = $"cfg:{_options.ClientId}|{_options.ApiKey}|{_options.ChecksumKey}";
        }

        if (_client == null || _clientFingerprint != fingerprint)
        {
            await _clientLock.WaitAsync();
            try
            {
                if (_client == null || _clientFingerprint != fingerprint)
                {
                    _logger.LogInformation(
                        "[PayOS] Credential change detected (fingerprint={FpPrefix}...) → rebuild PayOSClient",
                        fingerprint.Length > 24 ? fingerprint[..24] : fingerprint);
                    _client = dbCred != null
                        ? new PayOSClient(dbCred.ClientId, dbCred.ApiKey, dbCred.ChecksumKey)
                        : new PayOSClient(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
                    _clientFingerprint = fingerprint;
                }
            }
            finally
            {
                _clientLock.Release();
            }
        }
        return _client;
    }

    /// <summary>URL trang chờ kết quả thanh toán trên frontend, kèm orderId để trang biết poll đơn nào.</summary>
    public string BuildReturnUrl(int orderId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.ReturnUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.ReturnUrl;
        return AppendQuery(baseUrl, $"orderId={orderId}");
    }

    public string BuildCancelUrl(int orderId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.CancelUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.CancelUrl;
        return AppendQuery(baseUrl, $"orderId={orderId}");
    }

    /// <summary>
    /// Return URL cho báo cáo chuyên sâu — PaymentReturn phân biệt bằng type=bespoke.
    /// Kèm workspaceId/projectId vì trang return cần chúng để gọi API confirm (không có trong orderId như scrape order).
    /// </summary>
    public string BuildBespokeReturnUrl(int requestId, int workspaceId, int projectId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.ReturnUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.ReturnUrl;
        return AppendQuery(baseUrl, $"type=bespoke&requestId={requestId}&workspaceId={workspaceId}&projectId={projectId}");
    }

    public string BuildBespokeCancelUrl(int requestId, int workspaceId, int projectId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.CancelUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.CancelUrl;
        return AppendQuery(baseUrl, $"type=bespoke&requestId={requestId}&workspaceId={workspaceId}&projectId={projectId}");
    }

    private static string AppendQuery(string url, string query) =>
        url.Contains('?') ? $"{url}&{query}" : $"{url}?{query}";

    public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(
        long orderCode,
        long amount,
        string description,
        string returnUrl,
        string cancelUrl)
    {
        var client = await GetClientAsync();
        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl
        };
        return await client.PaymentRequests.CreateAsync(request);
    }

    /// <summary>Tra cứu trạng thái payment link trên PayOS theo orderCode (nguồn tin cậy khi confirm từ return page).</summary>
    public async Task<PaymentLink?> GetPaymentLinkAsync(long orderCode)
    {
        try
        {
            var client = await GetClientAsync();
            return await client.PaymentRequests.GetAsync(orderCode);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Không tra cứu được PayOS payment link cho orderCode {OrderCode}", orderCode);
            return null;
        }
        catch (InvalidOperationException)
        {
            // PayOS chưa cấu hình
            return null;
        }
    }

    /// <summary>
    /// Verify chữ ký HMAC-SHA256 của webhook PayOS. Trả về null nếu chữ ký sai / payload hỏng —
    /// KHÔNG được tin dữ liệu webhook khi hàm này trả null.
    /// </summary>
    public async Task<WebhookData?> VerifyWebhookAsync(Webhook webhook)
    {
        if (webhook?.Data == null || string.IsNullOrEmpty(webhook.Signature))
        {
            _logger.LogWarning("Webhook thiếu Data hoặc Signature — bỏ qua.");
            return null;
        }

        try
        {
            var client = await GetClientAsync();
            var result = await client.Webhooks.VerifyAsync(webhook);
            _logger.LogInformation("Webhook PayOS verify thành công: orderCode={OrderCode}", webhook.Data.OrderCode);
            return result;
        }
        catch (WebhookException ex)
        {
            _logger.LogWarning(ex, "Webhook PayOS có chữ ký không hợp lệ (orderCode {OrderCode}). Message={Message}",
                webhook?.Data?.OrderCode, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi verify webhook PayOS: {Message}", ex.Message);
            return null;
        }
    }
}
