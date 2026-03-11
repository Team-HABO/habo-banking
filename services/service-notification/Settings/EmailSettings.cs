namespace service_notification.Settings;

public class EmailSettings
{
    public required string SmtpHost { get; set; }
    public required int SmtpPort { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
}

