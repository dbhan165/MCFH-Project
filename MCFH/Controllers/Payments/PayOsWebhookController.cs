using MCFH.Services;
using MCFH.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace MCFH.Controllers.Payments;

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
    public async Task<IActionResult> Webhook([FromBody] Webhook payload)
    {
        if (payload == null)
            return BadRequest();

        var data = await _payOs.VerifyWebhookAsync(payload);
        if (data == null)
        {
            // Chữ ký sai — không tin, không xử lý.
            _logger.LogWarning("Từ chối webhook PayOS: chữ ký không hợp lệ.");
            return BadRequest(new { message = "Invalid signature." });
        }

        await _scrapeOrders.HandlePayOsWebhookAsync(data);
        return Ok(new { success = true });
    }
}
