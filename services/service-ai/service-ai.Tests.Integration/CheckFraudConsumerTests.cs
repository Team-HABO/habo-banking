using FluentAssertions;
using MassTransit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using service_ai.Messages;
using service_ai.Models;

namespace service_ai.Tests.Integration;

[Trait("Category", "Integration")]
public class CheckFraudConsumerTests(CheckFraudConsumerFixture fixture)
    : IClassFixture<CheckFraudConsumerFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private static CheckFraud BuildRequest(string message = "normal transaction") => new()
    {
        Data = new CheckFraudData
        {
            Account = new AccountInfo { Guid = "acc-001", Name = "Alice", Type = "personal" },
            Receiver = new AccountInfo { Guid = "acc-002", Name = "Bob", Type = "personal" },
            Amount = "250.00",
            TransactionType = "transfer",
            OriginIpAddress = "192.168.1.1"
        },
        Metadata = new CheckFraudMetadata
        {
            MessageType = "CheckFraud",
            MessageTimestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid()
        }
    };

    [Fact]
    public async Task Consumer_WhenAiReturnsNoFraud_PublishesFraudChecked()
    {
        fixture.OpenRouterService
            .SendPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FraudCheckResult { IsFraud = false, Reason = "Normal pattern", RiskScore = 0.1 });

        await fixture.Bus.Publish(BuildRequest());

        var message = await fixture.FraudCheckedConsumer.WaitForMessageAsync(Timeout);

        message.Data.Account.Guid.Should().Be("acc-001");
        message.Data.Amount.Should().Be("250.00");
        message.Data.TransactionType.Should().Be("transfer");
    }

    [Fact]
    public async Task Consumer_WhenAiDetectsFraud_PublishesFraudNotification()
    {
        fixture.OpenRouterService
            .SendPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FraudCheckResult { IsFraud = true, Reason = "Unusual IP and high amount", RiskScore = 0.95 });

        await fixture.Bus.Publish(BuildRequest());

        var message = await fixture.FraudNotificationConsumer.WaitForMessageAsync(Timeout);

        message.Data.Message.Should().Contain("Unusual IP and high amount");
    }

    [Fact]
    public async Task Consumer_WhenAiServiceThrowsHttpException_PublishesFraudNotification()
    {
        fixture.OpenRouterService
            .SendPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("OpenRouter unreachable"));

        await fixture.Bus.Publish(BuildRequest());

        var message = await fixture.FraudNotificationConsumer.WaitForMessageAsync(Timeout);

        message.Data.Message.Should().Contain("AI service error");
    }
}
