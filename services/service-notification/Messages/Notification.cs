using MassTransit;

namespace service_notification.Messages;

/// <summary>
/// Generic notification message consumed from the habo.banking:Notification exchange.
/// All services that need to trigger an email publish to this shared exchange.
/// <see cref="NotificationMetadata.MessageType"/> identifies the origin
/// (e.g. "FraudNotification", "ExchangeNotification") and drives the email subject.
/// </summary>
[EntityName("habo.banking:Notification")]
[MessageUrn("habo.banking:Notification")]
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

