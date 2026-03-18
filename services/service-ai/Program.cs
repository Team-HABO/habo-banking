using MassTransit;
using Serilog;
using service_ai.Consumers;
using service_ai.Services;


var builder = Host.CreateApplicationBuilder(args);

// Load environment variables from service .env file
DotNetEnv.Env.Load();

var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
    ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not set. Make sure it is defined in the .env file.");

var rabbitMqUsername = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
    ?? throw new InvalidOperationException("RABBITMQ_USERNAME is not set. Make sure it is defined in the .env file.");

var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
    ?? throw new InvalidOperationException("RABBITMQ_PASSWORD is not set. Make sure it is defined in the .env file.");

var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
    ?? throw new InvalidOperationException("RABBITMQ_HOST is not set. Make sure it is defined in the .env file.");

builder.Configuration["OpenRouter:ApiKey"] = openRouterApiKey;

// Configure Serilog - better logger than default one
builder.Services.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/service-ai-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger());

// Register OpenRouter AI service
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

// Configure MassTransit - Message Bus library
builder.Services.AddMassTransit(config =>
{
    // Register consumer - MassTransit uses this to know what messages to subscribe to
    config.AddConsumer<CheckFraudConsumer>();
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
        }
    );
});


var host = builder.Build();
host.Run();