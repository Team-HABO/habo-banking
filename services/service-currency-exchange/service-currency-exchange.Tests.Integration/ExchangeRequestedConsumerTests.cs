using FluentAssertions;
using MassTransit;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using service_currency_exchange.Messages;

namespace service_currency_exchange.Tests.Integration;

[Trait("Category", "Integration")]
public class ExchangeRequestedConsumerTests(ExchangeRequestedConsumerFixture fixture)
    : IClassFixture<ExchangeRequestedConsumerFixture>, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    // Reset shared state before every test so messages/stub config don't bleed across tests.
    public Task InitializeAsync()
    {
        fixture.ExchangeProcessedConsumer.Reset();
        fixture.ExchangeNotificationConsumer.Reset();
        fixture.CurrencyService.ClearSubstitute();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ExchangeRequested BuildRequest(string currency = "USD") => new()
    {
        Data = new ExchangeRequestedData
        {
            OwnerId = "123e4567-e89b-12d3-a456-426614174000",
            AccountGuid = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            Amount = "1000",
            Currency = currency,
            TransactionType = "exchange"
        },
        Metadata = new ExchangeRequestedMetadata
        {
            MessageType = "TRANSACTION_EXCHANGE",
            MessageTimestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid()
        }
    };

    [Fact]
    public async Task Consumer_WhenRateResolved_PublishesExchangeProcessed()
    {
        fixture.CurrencyService
            .GetRateFromDkkAsync("USD", Arg.Any<CancellationToken>())
            .Returns(0.1425m);

        await fixture.Bus.Publish(BuildRequest("USD"), x => x.SetRoutingKey("currency-exchange-requests-queue"));

        var message = await fixture.ExchangeProcessedConsumer.WaitForMessageAsync(Timeout);

        message.Data.OwnerId.Should().Be("123e4567-e89b-12d3-a456-426614174000");
        message.Data.AccountGuid.Should().Be("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        message.Data.Currency.Should().Be("USD");
        message.Data.ExchangeRate.Should().BeApproximately(0.1425, 0.0001);
    }

    [Fact]
    public async Task Consumer_WhenRateUnavailable_PublishesExchangeNotification()
    {
        fixture.CurrencyService
            .GetRateFromDkkAsync("XYZ", Arg.Any<CancellationToken>())
            .Returns((decimal?)null);

        await fixture.Bus.Publish(BuildRequest("XYZ"), x => x.SetRoutingKey("currency-exchange-requests-queue"));

        var message = await fixture.ExchangeNotificationConsumer.WaitForMessageAsync(Timeout);

        message.Data.Message.Should().Contain("XYZ");
    }

    [Fact]
    public async Task Consumer_WhenCurrencyServiceThrows_PublishesExchangeNotification()
    {
        fixture.CurrencyService
            .GetRateFromDkkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Frankfurter unreachable"));

        await fixture.Bus.Publish(BuildRequest("EUR"), x => x.SetRoutingKey("currency-exchange-requests-queue"));

        var message = await fixture.ExchangeNotificationConsumer.WaitForMessageAsync(Timeout);

        message.Data.Message.Should().NotBeNullOrEmpty();
    }
}
