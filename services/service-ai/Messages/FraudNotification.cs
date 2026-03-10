namespace service_ai.Messages;

/// <summary>
/// Published to the Notification-Service when fraud is detected (contract ID 5, step 2.5).
/// </summary>
[EntityName("habo.banking:FraudNotification")]
[MessageUrn("habo.banking:FraudNotification")]
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
