using FluentAssertions;
using service_notification.Messages;

namespace service_notification.Tests.Integration;

[Trait("Category", "Integration")]
public class FraudNotificationConsumerTests(RabbitMqConsumerFixture fixture)
    : IClassFixture<RabbitMqConsumerFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Consumer_WhenFraudNotificationPublished_SendsEmailWithCorrectSubject()
    {
        fixture.CapturingEmailService.Reset();

        var notification = new FraudNotification
        {
            Data = new FraudNotificationData { Message = "Suspicious transaction detected on account 12345." },
            Metadata = new FraudNotificationMetadata
            {
                MessageType = "FraudNotification",
                MessageTimestamp = DateTime.UtcNow
            }
        };

        await fixture.Bus.Publish(notification);

        var email = await fixture.CapturingEmailService.WaitForCallAsync(Timeout);

        email.Subject.Should().Be("HABO Bank - Potential fraud notification!");
        email.HtmlBody.Should().Contain("Suspicious transaction detected on account 12345.");
    }
}