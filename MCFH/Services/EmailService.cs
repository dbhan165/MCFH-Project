using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MCFH.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
}

public class EmailService : IEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var fromAddress = _config["Smtp:FromAddress"];
        var fromName = _config["Smtp:FromName"] ?? "MCFH System Hub";
        var apiKey = _config["Smtp:ApiKey"];

        if (string.IsNullOrWhiteSpace(fromAddress)
            || fromAddress.StartsWith("REPLACE_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Smtp:FromAddress chưa cấu hình. Dùng email sender đã verified trên Brevo.");
        }

        // Nếu có ApiKey (dạng xkeysib-...) thì dùng Brevo HTTP API.
        // SMTP key (xsmtpsib-...) phải để ở Password + Username = login @smtp-brevo.com
        if (!string.IsNullOrWhiteSpace(apiKey)
            && !apiKey.StartsWith("REPLACE_", StringComparison.Ordinal)
            && !apiKey.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase))
        {
            await SendViaBrevoApiAsync(toEmail, subject, htmlMessage, fromAddress, fromName, apiKey);
            return;
        }

        await SendViaSmtpAsync(toEmail, subject, htmlMessage, fromAddress, fromName);
    }

    private async Task SendViaBrevoApiAsync(
        string toEmail,
        string subject,
        string htmlMessage,
        string fromAddress,
        string fromName,
        string apiKey)
    {
        var payload = new
        {
            sender = new { name = fromName, email = fromAddress },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = htmlMessage
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient(nameof(EmailService));
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Brevo API gửi email thất bại ({Status}): {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                "Không thể gửi email qua Brevo API. Kiểm tra ApiKey và sender đã verified.");
        }

        _logger.LogInformation("Đã gửi email (Brevo API) tới {ToEmail}", toEmail);
    }

    private async Task SendViaSmtpAsync(
        string toEmail,
        string subject,
        string htmlMessage,
        string fromAddress,
        string fromName)
    {
        var smtpHost = _config["Smtp:Host"] ?? "smtp-relay.brevo.com";
        var smtpPortText = _config["Smtp:Port"] ?? "587";
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || username.StartsWith("REPLACE_", StringComparison.Ordinal)
            || password.StartsWith("REPLACE_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Chưa có Smtp:ApiKey hoặc SMTP Username/Password. Thêm ApiKey Brevo vào appsettings.Development.json.");
        }

        if (!int.TryParse(smtpPortText, out var smtpPort))
        {
            throw new InvalidOperationException("Smtp:Port không hợp lệ.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlMessage };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username.Trim(), password.Trim());
            await client.SendAsync(message);
            _logger.LogInformation("Đã gửi email (SMTP) tới {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email SMTP tới {ToEmail} thất bại", toEmail);
            throw new InvalidOperationException(
                "Không thể gửi email SMTP. Dùng Smtp:ApiKey (khuyến nghị) hoặc kiểm tra SMTP Login/Key trên Brevo.", ex);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
