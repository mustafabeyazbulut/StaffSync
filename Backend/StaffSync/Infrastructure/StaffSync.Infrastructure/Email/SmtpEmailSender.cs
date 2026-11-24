using Microsoft.Extensions.Options;
using StaffSync.Application.Interfaces.Email;
using System.Net;
using System.Net.Mail;

namespace StaffSync.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings settings;

        public SmtpEmailSender(IOptions<SmtpSettings> options) => settings = options.Value;

        public async Task SendAsync(IEnumerable<string> to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("Smtp:Host ayarı boş; e-posta gönderilemez.");

            using var message = new MailMessage { From = new MailAddress(settings.From), Subject = subject, Body = body };
            foreach (string address in to) message.To.Add(address);

            using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
            if (!string.IsNullOrEmpty(settings.UserName))
                client.Credentials = new NetworkCredential(settings.UserName, settings.Password);

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
