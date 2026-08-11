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
    private readonly IProviderCredentialResolver _resolver;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory,
        IProviderCredentialResolver resolver)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _resolver = resolver;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        // Ưu tiên BrevoKey default trong DB (admin đã cấu hình).
        // Nếu DB không có → fallback appsettings.json (giữ behavior cho dev).
        var resolved = await _resolver.ResolveBrevoDefaultAsync();
        if (resolved == null)
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Brevo key. Thêm key mặc định qua Admin → Cài đặt hệ thống → Brevo Email Key, " +
                "hoặc điền Smtp:FromAddress/Smtp:ApiKey vào appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(resolved.FromAddress))
        {
            throw new InvalidOperationException(
                "Brevo key không có FromAddress. Hãy cấu hình FromAddress đã verified trên Brevo.");
        }

        if (string.Equals(resolved.KeyType, "api", StringComparison.OrdinalIgnoreCase))
        {
            await SendViaBrevoApiAsync(
                toEmail, subject, htmlMessage,
                resolved.FromAddress, resolved.FromName, resolved.ApiKey);
        }
        else
        {
            await SendViaSmtpAsync(
                toEmail, subject, htmlMessage,
                resolved.FromAddress, resolved.FromName,
                resolved.SmtpLogin ?? resolved.ApiKey, // username
                resolved.ApiKey,                       // password
                resolved.SmtpHost ?? "smtp-relay.brevo.com",
                resolved.SmtpPort ?? 587);
        }
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
        string fromName,
        string username,
        string password,
        string smtpHost,
        int smtpPort)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Brevo key loại SMTP thiếu username hoặc password.");
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
                "Không thể gửi email SMTP. Kiểm tra SMTP Login/Key trên Brevo và FromAddress đã verified.", ex);
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

