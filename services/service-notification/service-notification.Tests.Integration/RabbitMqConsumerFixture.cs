using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using service_notification.Consumers;
using service_notification.Services;
using service_notification.Settings;
using Testcontainers.RabbitMq;

namespace service_notification.Tests.Integration;

/// <summary>
/// Shared fixture that starts a real RabbitMQ container (via Testcontainers) and an
/// <see cref="IHost"/> with MassTransit wired up to it, so the full consumer
/// pipeline can be exercised in integration tests.
/// </summary>
public sealed class RabbitMqConsumerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost _host = null!;

    public IBus Bus { get; private set; } = null!;
    public CapturingEmailService CapturingEmailService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start RabbitMQ container first so we have the connection string
        await _container.StartAsync();

        // Load SMTP settings from .env (same pattern as EmailServiceFixture)
        DotNetEnv.Env.TraversePath().Load();

        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException("SMTP_HOST is not set. Ensure the .env file is present.");
        var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
        var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")
            ?? throw new InvalidOperationException("SMTP_USERNAME is not set.");
        var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException("SMTP_PASSWORD is not set.");
        var smtpFromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
            ?? throw new InvalidOperationException("SMTP_FROM_EMAIL is not set.");
        var smtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? throw new InvalidOperationException("SMTP_FROM_NAME is not set.");

        var emailSettings = new EmailSettings
        {
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            Username = smtpUsername,
            Password = smtpPassword,
            FromEmail = smtpFromEmail,
            FromName = smtpFromName
        };

        var realEmailService = new EmailService(Options.Create(emailSettings));
        var capturingEmailService = new CapturingEmailService(realEmailService);
        CapturingEmailService = capturingEmailService;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Ensure Host.StartAsync waits for MassTransit bus/endpoints to be ready.
                services.Configure<MassTransitHostOptions>(options =>
                {
                    options.WaitUntilStarted = true;
                    options.StartTimeout = TimeSpan.FromSeconds(30);
                });

                services.Configure<EmailSettings>(opts =>
                {
                    opts.SmtpHost = emailSettings.SmtpHost;
                    opts.SmtpPort = emailSettings.SmtpPort;
                    opts.Username = emailSettings.Username;
                    opts.Password = emailSettings.Password;
                    opts.FromEmail = emailSettings.FromEmail;
                    opts.FromName = emailSettings.FromName;
                });

                // Register the capturing decorator as the IEmailService so the consumer
                // calls it. The decorator forwards every call to the real EmailService.
                services.AddSingleton<IEmailService>(capturingEmailService);

                services.AddMassTransit(x =>
                {
                    x.AddConsumer<NotificationConsumer>();
                    x.SetKebabCaseEndpointNameFormatter();

                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(_container.GetConnectionString());

                        // Notification uses [EntityName("habo.banking:Notification")].
                        // Bind this endpoint explicitly so Bus.Publish(Notification)
                        // reaches the consumer in integration tests.
                        cfg.ReceiveEndpoint("notification-consumer", e =>
                        {
                            e.Bind("habo.banking:Notification");
                            e.ConfigureConsumer<NotificationConsumer>(ctx);
                        });
                    });
                });
            })
            .Build();

        await _host.StartAsync();
        Bus = _host.Services.GetRequiredService<IBus>();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _container.DisposeAsync();
    }
}
