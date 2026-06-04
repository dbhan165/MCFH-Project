using System.Net;
using System.Net.Mail;

namespace MCFH.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"]!);
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var fromAddress = _config["Smtp:FromAddress"];
            var fromName = _config["Smtp:FromName"];

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = true; // Gmail bắt buộc dùng SSL/TLS

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress!, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true // Cho phép gửi giao diện HTML cho đẹp
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}