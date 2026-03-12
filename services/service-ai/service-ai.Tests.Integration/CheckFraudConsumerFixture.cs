using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using service_ai.Consumers;
using service_ai.Messages;
using service_ai.Services;
using Testcontainers.RabbitMq;

namespace service_ai.Tests.Integration;

/// <summary>
/// Shared fixture that starts a real RabbitMQ container and an <see cref="IHost"/> with
/// <see cref="CheckFraudConsumer"/> wired up to it. <see cref="IOpenRouterService"/> is
/// replaced with an NSubstitute mock so tests control the AI response without any
/// outbound HTTP calls.
/// </summary>
public sealed class CheckFraudConsumerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost _host = null!;

    public IOpenRouterService OpenRouterService { get; } = Substitute.For<IOpenRouterService>();

    // Pre-created singletons registered into DI so MassTransit resolves these exact instances
    public CapturingConsumer<FraudNotification> FraudNotificationConsumer { get; } = new();
    public CapturingConsumer<FraudChecked> FraudCheckedConsumer { get; } = new();

    public IBus Bus { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(OpenRouterService);

                // Register the pre-created capturing instances as singletons so MassTransit
                // resolves the same objects that the tests hold references to
                services.AddSingleton(FraudNotificationConsumer);
                services.AddSingleton(FraudCheckedConsumer);

                services.AddMassTransit(x =>
                {
                    x.AddConsumer<CheckFraudConsumer>();
                    x.AddConsumer<CapturingConsumer<FraudNotification>>();
                    x.AddConsumer<CapturingConsumer<FraudChecked>>();

                    x.SetKebabCaseEndpointNameFormatter();

                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(_container.GetConnectionString());
                        cfg.ConfigureEndpoints(ctx);
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
