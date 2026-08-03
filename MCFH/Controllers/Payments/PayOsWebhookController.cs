using MCFH.Services;
using MCFH.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using System.Text.Json.Serialization;

namespace MCFH.Controllers.Payments;

/// <summary>
/// Model nhận webhook từ PayOS — dùng JsonPropertyName để map đúng snake_case JSON sang PascalCase properties.
/// PayOS gửi: code, desc, data, signature, counterAccountBankId, counterAccountBankName, ...
/// SDK model dùng: Code, Description, Data, Signature, CounterAccountBankId, CounterAccountBankName, ...
/// </summary>
public class PayOsWebhookRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Description { get; set; } = "";

    [JsonPropertyName("data")]
    public PayOsWebhookData? Data { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = "";
}

public class PayOsWebhookData
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("transactionDateTime")]
    public string TransactionDateTime { get; set; } = "";

    [JsonPropertyName("paymentLinkId")]
    public string PaymentLinkId { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Description2 { get; set; } = "";

    [JsonPropertyName("counterAccountBankId")]
    public string CounterAccountBankId { get; set; } = "";

    [JsonPropertyName("counterAccountBankName")]
    public string CounterAccountBankName { get; set; } = "";

    [JsonPropertyName("counterAccountName")]
    public string CounterAccountName { get; set; } = "";

    [JsonPropertyName("counterAccountNumber")]
    public string CounterAccountNumber { get; set; } = "";

    [JsonPropertyName("virtualAccountName")]
    public string VirtualAccountName { get; set; } = "";

    [JsonPropertyName("virtualAccountNumber")]
    public string VirtualAccountNumber { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";
}

[ApiController]
[Route("api/payments/payos")]
public class PayOsWebhookController : ControllerBase
{
    private readonly PayOsService _payOs;
    private readonly ScrapeOrderService _scrapeOrders;
    private readonly ILogger<PayOsWebhookController> _logger;

    public PayOsWebhookController(
        PayOsService payOs,
        ScrapeOrderService scrapeOrders,
        ILogger<PayOsWebhookController> logger)
    {
        _payOs = payOs;
        _scrapeOrders = scrapeOrders;
        _logger = logger;
    }

    /// <summary>
    /// Webhook PayOS — nguồn tin cậy về thanh toán. Verify chữ ký HMAC trước khi tin dữ liệu.
    /// Luôn trả 200 cho payload hợp lệ (kể cả webhook test orderCode 123 khi đăng ký URL) để PayOS không retry vô ích.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] PayOsWebhookRequest payload)
    {
        if (payload == null)
            return BadRequest();

        _logger.LogInformation("Webhook PayOS nhận được: {@Payload}", payload);

        try
        {
            // Map sang Webhook model của SDK để VerifyAsync
            var webhook = new Webhook
            {
                Code = payload.Code,
                Description = payload.Description,
                Data = payload.Data != null ? new WebhookData
                {
                    OrderCode = payload.Data.OrderCode,
                    Amount = payload.Data.Amount,
                    Description = payload.Data.Description,
                    AccountNumber = payload.Data.AccountNumber,
                    Reference = payload.Data.Reference,
                    TransactionDateTime = payload.Data.TransactionDateTime,
                    PaymentLinkId = payload.Data.PaymentLinkId,
                    Code = payload.Data.Code,
                    Description2 = payload.Data.Description2,
                    CounterAccountBankId = payload.Data.CounterAccountBankId,
                    CounterAccountBankName = payload.Data.CounterAccountBankName,
                    CounterAccountName = payload.Data.CounterAccountName,
                    CounterAccountNumber = payload.Data.CounterAccountNumber,
                    VirtualAccountName = payload.Data.VirtualAccountName,
                    VirtualAccountNumber = payload.Data.VirtualAccountNumber,
                    Currency = payload.Data.Currency
                } : null,
                Signature = payload.Signature
            };

            var data = await _payOs.VerifyWebhookAsync(webhook);
            if (data == null)
            {
                _logger.LogWarning("Từ chối webhook PayOS: chữ ký không hợp lệ.");
                return BadRequest(new { message = "Invalid signature." });
            }

            await _scrapeOrders.HandlePayOsWebhookAsync(data);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý webhook PayOS: {Message}", ex.Message);
            return StatusCode(500, new { message = "Internal error", detail = ex.Message });
        }
    }
}
