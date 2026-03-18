﻿using MassTransit;

namespace service_currency_exchange.Messages;

/// <summary>
/// Published to Notification-Service when the currency exchange cannot be completed (contract ID 6, step 4.4).
/// Reuses the shared habo.banking:Notification exchange so the Notification-Service
/// receives it without any changes on its end.
/// </summary>
[EntityName("habo.banking:Notification")]
[MessageUrn("habo.banking:Notification")]
public record ExchangeNotification
{
    public ExchangeNotificationData Data { get; init; } = new();
    public ExchangeNotificationMetadata Metadata { get; init; } = new();
}

public record ExchangeNotificationData
{
    public string Message { get; init; } = string.Empty;
}

public record ExchangeNotificationMetadata
{
    public string MessageType { get; init; } = string.Empty;
    public DateTime MessageTimestamp { get; init; }
    public Guid MessageId { get; init; }
}

