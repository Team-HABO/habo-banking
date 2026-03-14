using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using service_currency_exchange.Consumers;
using service_currency_exchange.Messages;
using service_currency_exchange.Services;
using Testcontainers.RabbitMq;

namespace service_currency_exchange.Tests.Integration;

/// <summary>
/// Shared fixture that spins up a real RabbitMQ container via Testcontainers and an
/// <see cref="IHost"/> with <see cref="ExchangeRequestedConsumer"/> wired up.
/// <see cref="ICurrencyService"/> is replaced with an NSubstitute mock so tests control
/// the exchange-rate response without making real outbound HTTP calls to Frankfurter.
/// </summary>
public sealed class ExchangeRequestedConsumerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost _host = null!;

    /// <summary>NSubstitute mock — configure per-test with <c>.Returns(...)</c>.</summary>
    public ICurrencyService CurrencyService { get; } = Substitute.For<ICurrencyService>();

    // Pre-created singletons injected into DI so MassTransit resolves the exact instances
    // that test methods hold references to.
    public CapturingConsumer<ExchangeProcessed> ExchangeProcessedConsumer { get; } = new();
    public CapturingConsumer<ExchangeNotification> ExchangeNotificationConsumer { get; } = new();

    public IBus Bus { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(CurrencyService);

                // Ensure Host.StartAsync waits until MassTransit is connected and receive
                // endpoints are started, so the first test publish is not dropped.
                services.Configure<MassTransitHostOptions>(options =>
                {
                    options.WaitUntilStarted = true;
                    options.StartTimeout = TimeSpan.FromSeconds(30);
                });

                // Register pre-created capturing consumers as singletons so MassTransit
                // resolves the same instances the tests hold.
                services.AddSingleton(ExchangeProcessedConsumer);
                services.AddSingleton(ExchangeNotificationConsumer);

                services.AddMassTransit(x =>
                {
                    x.AddConsumer<ExchangeRequestedConsumer>();
                    x.AddConsumer<CapturingConsumer<ExchangeProcessed>>();
                    x.AddConsumer<CapturingConsumer<ExchangeNotification>>();

                    x.SetKebabCaseEndpointNameFormatter();

                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(_container.GetConnectionString());

                        // Bind ExchangeRequestedConsumer's queue to the exact exchange name
                        // declared via [EntityName] on ExchangeRequested, so Bus.Publish() reaches it.
                        cfg.ReceiveEndpoint("exchange-requested-consumer", e =>
                        {
                            e.Bind("habo.banking:CurrencyExchangeRequested");
                            e.ConfigureConsumer<ExchangeRequestedConsumer>(ctx);
                        });

                        // Bind capturing consumers to the exact exchange names declared via
                        // [EntityName] on ExchangeProcessed and ExchangeNotification respectively.
                        cfg.ReceiveEndpoint("test-capture-exchange-processed", e =>
                        {
                            e.Bind("habo.banking:CurrencyExchangeProcessed");
                            e.ConfigureConsumer<CapturingConsumer<ExchangeProcessed>>(ctx);
                        });

                        cfg.ReceiveEndpoint("test-capture-exchange-notification", e =>
                        {
                            e.Bind("habo.banking:Notification");
                            e.ConfigureConsumer<CapturingConsumer<ExchangeNotification>>(ctx);
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
