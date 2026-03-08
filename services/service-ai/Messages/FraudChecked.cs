namespace service_ai.Messages;

/// <summary>
/// Published to the Transaction-Service when fraud check passes (contract ID 5, step 3).
/// Carries the original transaction data through for processing.
/// </summary>
public record FraudChecked
{
    public FraudCheckedData Data { get; init; } = new();
    public FraudCheckedMetadata Metadata { get; init; } = new();
}

public record FraudCheckedData
{
    public AccountInfo Account { get; init; } = new();
    public AccountInfo? Receiver { get; init; }
    public string Amount { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
}

public record FraudCheckedMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
}
