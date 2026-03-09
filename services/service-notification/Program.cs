using DotNetEnv;
using MassTransit;
using Serilog;
using service_notification.Consumers;
using service_notification.Settings;
using service_notification.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog - better logger than default one
builder.Services.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/service-notification-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger());

// Load environment variables from root .env file
Env.TraversePath().Load();

var rabbitMqUsername = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
                       ?? throw new InvalidOperationException(
                           "RABBITMQ_USERNAME is not set. Make sure it is defined in the .env file.");

var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                       ?? throw new InvalidOperationException(
                           "RABBITMQ_PASSWORD is not set. Make sure it is defined in the .env file.");

var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                   ?? throw new InvalidOperationException(
                       "RABBITMQ_HOST is not set. Make sure it is defined in the .env file.");

var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
               ?? throw new InvalidOperationException(
                   "SMTP_HOST is not set. Make sure it is defined in the .env file.");

var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");

var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME")
                   ?? throw new InvalidOperationException(
                       "SMTP_USERNAME is not set. Make sure it is defined in the .env file.");

var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                   ?? throw new InvalidOperationException(
                       "SMTP_PASSWORD is not set. Make sure it is defined in the .env file.");

var smtpFromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
                    ?? throw new InvalidOperationException(
                        "SMTP_FROM_EMAIL is not set. Make sure it is defined in the .env file.");

var smtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
                   ?? throw new InvalidOperationException(
                       "SMTP_FROM_NAME is not set. Make sure it is defined in the .env file.");

builder.Services.Configure<EmailSettings>(options =>
{
    options.SmtpHost = smtpHost;
    options.SmtpPort = smtpPort;
    options.Username = smtpUsername;
    options.Password = smtpPassword;
    options.FromEmail = smtpFromEmail;
    options.FromName = smtpFromName;
});

builder.Services.AddTransient<IEmailService, EmailService>();

// Configure MassTransit - Message Bus library
builder.Services.AddMassTransit(config =>
{
    // Register consumer - MassTransit uses this to know what messages to subscribe to
    config.AddConsumer<FraudNotificationConsumer>();
    config.SetKebabCaseEndpointNameFormatter();

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", host =>
        {
            host.Username(rabbitMqUsername);
            host.Password(rabbitMqPassword);
        });

        // Automatically creates and binds queues based on registered consumers
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();