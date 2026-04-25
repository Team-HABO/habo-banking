﻿using MassTransit;

namespace service_currency_exchange.Messages;

/// <summary>
/// Consumed from Transaction-Service when a currency exchange is requested (contract ID 6, step 3).
/// </summary>
[EntityName("currency-exchange-events")]
[MessageUrn("currency-exchange-events")]
public record ExchangeRequested
{
    public ExchangeRequestedData Data { get; init; } = new();
    public ExchangeRequestedMetadata Metadata { get; init; } = new();
}

public record ExchangeRequestedData
{
    public string OwnerId { get; init; } = string.Empty;
    public string AccountGuid { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;

    /// <summary>Target currency to exchange into (from DKK).</summary>
    public string Currency { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
}

public record ExchangeRequestedMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}

