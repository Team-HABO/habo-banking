using MassTransit;

namespace service_notification.Messages;

/// <summary>
/// Generic notification message consumed from the habo.banking:Notification exchange.
/// All services that need to trigger an email publish to this shared exchange.
/// <see cref="NotificationMetadata.MessageType"/> carries the originating transaction type
/// (e.g. "TRANSACTION_EXCHANGE", "TRANSACTION_TRANSFER", "TRANSACTION_WITHDRAW", "TRANSACTION_DEPOSIT")
/// and drives the email subject line selection in the NotificationConsumer.
/// </summary>
[EntityName("notification-events")]
[MessageUrn("notification-events")]
public record Notification
{
    public NotificationData Data { get; init; } = new();
    public NotificationMetadata Metadata { get; init; } = new();
}

public record NotificationData
{
    public string Message { get; init; } = string.Empty;
}

public record NotificationMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}

