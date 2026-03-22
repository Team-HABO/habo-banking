using MassTransit;
using Serilog;
using service_currency_exchange.Consumers;
using service_currency_exchange.Services;

var builder = Host.CreateApplicationBuilder(args);

// Load environment variables from service .env file
DotNetEnv.Env.NoClobber().Load();

var rabbitMqUsername = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
    ?? throw new InvalidOperationException("RABBITMQ_USERNAME is not set. Make sure it is defined in the .env file.");

var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
    ?? throw new InvalidOperationException("RABBITMQ_PASSWORD is not set. Make sure it is defined in the .env file.");

var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
    ?? throw new InvalidOperationException("RABBITMQ_HOST is not set. Make sure it is defined in the .env file.");

// Configure Serilog
builder.Services.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/service-currency-exchange-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger());

// Register Frankfurter HTTP client
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Frankfurter:BaseUrl"]
        ?? "https://api.frankfurter.dev/v1/");
});

// Configure MassTransit
builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<ExchangeRequestedConsumer>();
    config.SetKebabCaseEndpointNameFormatter();

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", host =>
        {
            host.Username(rabbitMqUsername);
            host.Password(rabbitMqPassword);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
