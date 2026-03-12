using FluentAssertions;
using service_notification.Messages;

namespace service_notification.Tests.Integration;

[Trait("Category", "Integration")]
public class NotificationConsumerTests(RabbitMqConsumerFixture fixture)
    : IClassFixture<RabbitMqConsumerFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Consumer_WhenFraudNotificationPublished_SendsEmailWithFraudSubject()
    {
        fixture.CapturingEmailService.Reset();

        await fixture.Bus.Publish(new Notification
        {
            Data = new NotificationData { Message = "Suspicious transaction detected on account 12345." },
            Metadata = new NotificationMetadata
            {
                MessageType = "FraudNotification",
                MessageTimestamp = DateTime.UtcNow
            }
        });

        var email = await fixture.CapturingEmailService.WaitForCallAsync(Timeout);

        email.Subject.Should().Be("HABO Bank - Potential fraud notification!");
        email.HtmlBody.Should().Contain("Suspicious transaction detected on account 12345.");
    }

    [Fact]
    public async Task Consumer_WhenExchangeNotificationPublished_SendsEmailWithExchangeSubject()
    {
        fixture.CapturingEmailService.Reset();

        await fixture.Bus.Publish(new Notification
        {
            Data = new NotificationData { Message = "Currency exchange for account 67890 could not be completed." },
            Metadata = new NotificationMetadata
            {
                MessageType = "ExchangeNotification",
                MessageTimestamp = DateTime.UtcNow
            }
        });

        var email = await fixture.CapturingEmailService.WaitForCallAsync(Timeout);

        email.Subject.Should().Be("HABO Bank - Currency exchange failed!");
        email.HtmlBody.Should().Contain("Currency exchange for account 67890 could not be completed.");
    }

    [Fact]
    public async Task Consumer_WhenUnknownMessageTypePublished_SendsEmailWithFallbackSubject()
    {
        fixture.CapturingEmailService.Reset();

        await fixture.Bus.Publish(new Notification
        {
            Data = new NotificationData { Message = "Something happened." },
            Metadata = new NotificationMetadata
            {
                MessageType = "UnknownType",
                MessageTimestamp = DateTime.UtcNow
            }
        });

        var email = await fixture.CapturingEmailService.WaitForCallAsync(Timeout);

        email.Subject.Should().Be("HABO Bank - Notification");
        email.HtmlBody.Should().Contain("Something happened.");
    }
}