namespace service_notification.Services;

public interface IEmailService
{
    Task<string> SendEmailAsync(string toEmail, string toName,
        string subject, string htmlBody);
}
