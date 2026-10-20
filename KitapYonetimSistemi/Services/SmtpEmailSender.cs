using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace KitapYonetimSistemi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // E-posta göndermek için gereken ayarların hepsi tanımlı mı?
        public static bool YapilandirmaVarMi(IConfiguration configuration) =>
            !string.IsNullOrEmpty(configuration["Smtp:Host"]) &&
            !string.IsNullOrEmpty(configuration["Smtp:User"]) &&
            !string.IsNullOrEmpty(configuration["Smtp:Password"]);

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["Smtp:Host"]!;
            var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
            var user = _configuration["Smtp:User"]!;
            var password = _configuration["Smtp:Password"]!;
            var from = _configuration["Smtp:From"] ?? user;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, password)
            };

            using var mesaj = new MailMessage
            {
                From = new MailAddress(from, "Satır Arası"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mesaj.To.Add(email);

            try
            {
                await client.SendMailAsync(mesaj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gönderilemedi: {Email}", email);
                throw;
            }
        }
    }
}
