using MassTransit;
using service_currency_exchange.Messages;
using service_currency_exchange.Services;

namespace service_currency_exchange.Consumers;

public class ExchangeRequestedConsumer(
    ILogger<ExchangeRequestedConsumer> logger,
    ICurrencyService currencyService) : IConsumer<ExchangeRequested>
{
    public async Task Consume(ConsumeContext<ExchangeRequested> context)
    {
        var request = context.Message;
        var data = request.Data;

        logger.LogInformation(
            "Received exchange request for account {AccountGuid}: {Amount} DKK → {Currency}",
            data.AccountGuid, data.Amount, data.Currency);

        try
        {
            var rate = await currencyService.GetRateFromDkkAsync(data.Currency, context.CancellationToken);

            if (rate is null)
            {
                logger.LogWarning(
                    "Exchange rate unavailable for {Currency} (account {AccountGuid}). Publishing failure notification.",
                    data.Currency, data.AccountGuid);

                await context.Publish(new ExchangeNotification
                {
                    Data = new ExchangeNotificationData
                    {
                        Message = $"Currency exchange failed: rate for '{data.Currency}' could not be retrieved."
                    },
                    Metadata = new ExchangeNotificationMetadata
                    {
                        MessageType = request.Metadata.MessageType,
                        MessageTimestamp = DateTime.UtcNow,
                        MessageId = request.Metadata.MessageId
                    }
                }, x => x.SetRoutingKey("notification-queue"));

                return;
            }

            logger.LogInformation(
                "Exchange rate resolved for account {AccountGuid}: 1 DKK = {Rate} {Currency}. Publishing ExchangeProcessed.",
                data.AccountGuid, rate, data.Currency);

            await context.Publish(new ExchangeProcessed
            {
                Data = new ExchangeProcessedData
                {
                    OwnerId = data.OwnerId,
                    AccountGuid = data.AccountGuid,
                    Amount = data.Amount,
                    Currency = data.Currency,
                    TransactionType = data.TransactionType,
                    ExchangeRate = (double)rate.Value
                },
                Metadata = new ExchangeProcessedMetadata
                {
                    MessageType = request.Metadata.MessageType,
                    MessageTimestamp = DateTime.UtcNow,
                    MessageId = request.Metadata.MessageId
                }
            }, x => x.SetRoutingKey("currency-exchange-response-queue"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing exchange for account {AccountGuid}", data.AccountGuid);

            await context.Publish(new ExchangeNotification
            {
                Data = new ExchangeNotificationData
                {
                    Message = "Currency exchange failed due to an unexpected error."
                },
                Metadata = new ExchangeNotificationMetadata
                {
                    MessageType = request.Metadata.MessageType,
                    MessageTimestamp = DateTime.UtcNow,
                    MessageId = request.Metadata.MessageId
                }
            }, x => x.SetRoutingKey("notification-queue"));
        }

        logger.LogInformation("Exchange request processing complete for account {AccountGuid}", data.AccountGuid);
    }
}


