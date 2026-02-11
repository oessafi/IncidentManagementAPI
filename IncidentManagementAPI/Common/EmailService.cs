using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace IncidentManagementAPI.Common
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendOtpAsync(string email, string otp)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = "Code de vérification (OTP)",
                Body = $@"
Bonjour,

Votre code de vérification est :

OTP : {otp}

Ce code est valable 5 minutes.
Ne le partagez avec personne.

Incident Management
",
                IsBodyHtml = false
            };

            message.To.Add(email);

            using var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await smtp.SendMailAsync(message);
        }
    }
}
