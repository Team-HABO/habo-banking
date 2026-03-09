namespace service_notification.Messages;

/// <summary>
/// Consumed from the message bus when fraud is detected by service-ai.
/// </summary>
public record FraudNotification
{
    public FraudNotificationData Data { get; init; } = new();
    public FraudNotificationMetadata Metadata { get; init; } = new();
}

public record FraudNotificationData
{
    public string Message { get; init; } = string.Empty;
}

public record FraudNotificationMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
}

