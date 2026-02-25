using MassTransit;
using Serilog;
using service_ai.Consumers;
using service_ai.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog - better logger than default one
builder.Services.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/service-ai.log", rollingInterval: RollingInterval.Day)
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
            cfg.Host("localhost", "/", host =>
            {
                host.Username("guest");
                host.Password("guest");
            });

            // Automatically creates and binds queues based on registered consumers
            cfg.ConfigureEndpoints(context);
        }
    );
});

// builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();