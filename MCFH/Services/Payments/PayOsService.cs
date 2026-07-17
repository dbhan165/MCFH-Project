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
/// </summary>
public class PayOsService
{
    private readonly PayOsOptions _options;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<PayOsService> _logger;
    private readonly Lazy<PayOSClient> _client;

    public PayOsService(
        IOptions<PayOsOptions> options,
        IOptions<AuthOptions> authOptions,
        ILogger<PayOsService> logger)
    {
        _options = options.Value;
        _authOptions = authOptions.Value;
        _logger = logger;
        _client = new Lazy<PayOSClient>(() =>
        {
            if (!_options.IsConfigured)
                throw new InvalidOperationException(
                    "PayOS chưa được cấu hình. Thiết lập PayOS:ClientId / ApiKey / ChecksumKey trong appsettings.Development.json hoặc biến môi trường.");
            return new PayOSClient(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
        });
    }

    public bool IsConfigured => _options.IsConfigured;

    /// <summary>URL trang chờ kết quả thanh toán trên frontend, kèm orderId để trang biết poll đơn nào.</summary>
    public string BuildReturnUrl(int orderId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.ReturnUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.ReturnUrl;
        return AppendOrderId(baseUrl, orderId);
    }

    public string BuildCancelUrl(int orderId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.CancelUrl)
            ? $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/payment/return"
            : _options.CancelUrl;
        return AppendOrderId(baseUrl, orderId);
    }

    private static string AppendOrderId(string url, int orderId) =>
        url.Contains('?') ? $"{url}&orderId={orderId}" : $"{url}?orderId={orderId}";

    public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(
        long orderCode,
        long amount,
        string description,
        string returnUrl,
        string cancelUrl)
    {
        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl
        };
        return await _client.Value.PaymentRequests.CreateAsync(request);
    }

    /// <summary>Tra cứu trạng thái payment link trên PayOS theo orderCode (nguồn tin cậy khi confirm từ return page).</summary>
    public async Task<PaymentLink?> GetPaymentLinkAsync(long orderCode)
    {
        try
        {
            return await _client.Value.PaymentRequests.GetAsync(orderCode);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Không tra cứu được PayOS payment link cho orderCode {OrderCode}", orderCode);
            return null;
        }
    }

    /// <summary>
    /// Verify chữ ký HMAC-SHA256 của webhook PayOS. Trả về null nếu chữ ký sai / payload hỏng —
    /// KHÔNG được tin dữ liệu webhook khi hàm này trả null.
    /// </summary>
    public async Task<WebhookData?> VerifyWebhookAsync(Webhook webhook)
    {
        try
        {
            return await _client.Value.Webhooks.VerifyAsync(webhook);
        }
        catch (WebhookException ex)
        {
            _logger.LogWarning(ex, "Webhook PayOS có chữ ký không hợp lệ (orderCode {OrderCode})", webhook?.Data?.OrderCode);
            return null;
        }
    }
}
