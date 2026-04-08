using MassTransit;

namespace service_currency_exchange.Messages;

/// <summary>
/// Published back to Transaction-Service after a successful exchange rate lookup.
/// </summary>
[EntityName("currency-exchange-events")]
[MessageUrn("currency-exchange-events")]
public record ExchangeProcessed
{
    public ExchangeProcessedData Data { get; init; } = new();
    public ExchangeProcessedMetadata Metadata { get; init; } = new();
}

public record ExchangeProcessedData
{
    public string OwnerId { get; init; } = string.Empty;
    public string AccountGuid { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;

    /// <summary>Exchange rate from DKK to the target currency.</summary>
    public double ExchangeRate { get; init; }
}

public record ExchangeProcessedMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}


