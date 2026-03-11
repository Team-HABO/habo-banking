using Microsoft.Extensions.Options;
using service_notification.Services;
using service_notification.Settings;

namespace service_notification.Tests.Integration;

public class EmailServiceFixture : IAsyncLifetime
{
    public IEmailService EmailService { get; private set; } = null!;
    public string ToEmail { get; private set; } = string.Empty;
    public string ToName { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        // Walk up from the test runner working directory to find the .env at the repo root
        DotNetEnv.Env.TraversePath().Load();

        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException("SMTP_HOST is not set. Ensure the .env file is present.");

        var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");

        var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")
            ?? throw new InvalidOperationException("SMTP_USERNAME is not set. Ensure the .env file is present.");

        var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException("SMTP_PASSWORD is not set. Ensure the .env file is present.");

        var smtpFromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
            ?? throw new InvalidOperationException("SMTP_FROM_EMAIL is not set. Ensure the .env file is present.");

        var smtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? throw new InvalidOperationException("SMTP_FROM_NAME is not set. Ensure the .env file is present.");

        ToEmail = smtpFromEmail;
        ToName = smtpFromName;

        var emailSettings = new EmailSettings
        {
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            Username = smtpUsername,
            Password = smtpPassword,
            FromEmail = smtpFromEmail,
            FromName = smtpFromName
        };

        EmailService = new EmailService(Options.Create(emailSettings));

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

