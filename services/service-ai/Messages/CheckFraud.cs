namespace service_ai.Messages;

public record CheckFraud
{
    public CheckFraudData Data { get; init; } = new();
    public CheckFraudMetadata Metadata { get; init; } = new();
}

public record CheckFraudData
{
    public string OwnerId { get; init; } = string.Empty;
    public AccountInfo Account { get; init; } = new();
    public AccountInfo? Receiver { get; init; }
    public string Amount { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string OriginIpAddress { get; init; } = string.Empty;
}

public record CheckFraudMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}
