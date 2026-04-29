using MassTransit;

namespace service_ai.Messages;

/// <summary>
/// Published to the Transaction-Service when fraud check passes.
/// Carries the original transaction data through for processing.
/// </summary>
[EntityName("service_ai.Messages:FraudChecked")]
[MessageUrn("service_ai.Messages:FraudChecked")]
public record FraudChecked
{
    public FraudCheckedData Data { get; init; } = new();
    public FraudCheckedMetadata Metadata { get; init; } = new();
}

public record FraudCheckedData
{
    public string OwnerId { get; init; } = string.Empty;
    public AccountInfo Account { get; init; } = new();
    public AccountInfo? Receiver { get; init; }
    public string Amount { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string? Currency { get; init; }
}

public record FraudCheckedMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}
