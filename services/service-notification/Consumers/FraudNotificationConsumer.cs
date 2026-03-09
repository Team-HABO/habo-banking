using MassTransit;
using service_notification.Messages;

namespace service_notification.Consumers;

public class FraudNotificationConsumer(ILogger<FraudNotificationConsumer> logger) : IConsumer<FraudNotification>
{
    public Task Consume(ConsumeContext<FraudNotification> context)
    {
        var notification = context.Message;

        logger.LogInformation(
            "Received fraud notification at {Timestamp}: {Message}",
            notification.Metadata.MessageTimestamp,
            notification.Data.Message);

        // TODO: implement actual notification logic (e.g. email, SMS)

        return Task.CompletedTask;
    }
}
