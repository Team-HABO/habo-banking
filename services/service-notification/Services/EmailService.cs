using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using service_notification.Settings;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace service_notification.Services;

public class EmailService(IOptions<EmailSettings> settings) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task<string> SendEmailAsync(string toEmail, string toName,
        string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort,
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        var response = await client.SendAsync(message);
        await client.DisconnectAsync(true);

        return response;
    }
}