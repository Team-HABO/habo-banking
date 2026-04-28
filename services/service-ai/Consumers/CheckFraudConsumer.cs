using MassTransit;
using service_ai.Messages;
using service_ai.Services;

namespace service_ai.Consumers;

public class CheckFraudConsumer(
    ILogger<CheckFraudConsumer> logger,
    IOpenRouterService openRouterService) : IConsumer<CheckFraud>
{
    public async Task Consume(ConsumeContext<CheckFraud> context)
    {
        var request = context.Message;
        var data = request.Data;
        logger.LogInformation("Received fraud check request for account {AccountGuid}, type {TransactionType}, amount {Amount}",
            data.Account.Guid, data.TransactionType, data.Amount);

        // Build the user prompt from transaction fields
        var prompt = $"Amount: {data.Amount}\n" +
                     $"Transaction Type: {data.TransactionType}\n" +
                     $"Origin IP Address: {data.OriginIpAddress}";

        // Send the prompt to OpenRouter AI
        try
        {
            var result = await openRouterService.SendPromptAsync(prompt, context.CancellationToken);

            if (result.IsFraud)
            {
                logger.LogWarning(
                    "Fraud detected for account {AccountGuid}: {Reason} (risk score: {RiskScore})",
                    data.Account.Guid, result.Reason, result.RiskScore);

                await context.Publish(new FraudNotification
                {
                    Data = new FraudNotificationData
                    {
                        Message = $"Fraudulent transaction blocked: {result.Reason}"
                    },
                    Metadata = new FraudNotificationMetadata
                    {
                        MessageType = request.Metadata.MessageType,
                        MessageTimestamp = DateTime.UtcNow,
                        MessageId = request.Metadata.MessageId
                    }
                }, x => x.SetRoutingKey("notification-queue"));

                logger.LogInformation("Published fraud notification for account {AccountGuid}", data.Account.Guid);
            }
            else
            {
                logger.LogInformation(
                    "No fraud detected for account {AccountGuid} (risk score: {RiskScore})",
                    data.Account.Guid, result.RiskScore);

                await context.Publish(new FraudChecked
                {
                    Data = new FraudCheckedData
                    {
                        OwnerId = data.OwnerId,
                        Account = data.Account,
                        Receiver = data.Receiver,
                        Amount = data.Amount,
                        TransactionType = data.TransactionType,
                        Currency = data.Currency
                    },
                    Metadata = new FraudCheckedMetadata
                    {
                        MessageType = request.Metadata.MessageType,
                        MessageTimestamp = DateTime.UtcNow,
                        MessageId = request.Metadata.MessageId
                    }
                });

                logger.LogInformation("Published fraud-cleared transaction for account {AccountGuid}", data.Account.Guid);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OpenRouter request failed for fraud check on account {AccountGuid}. Blocking transaction due to uncertainty.", data.Account.Guid);

            await context.Publish(new FraudNotification
            {
                Data = new FraudNotificationData
                {
                    Message = "Transaction blocked: fraud check could not be completed due to an AI service error."
                },
                Metadata = new FraudNotificationMetadata
                {
                    MessageType = request.Metadata.MessageType,
                    MessageTimestamp = DateTime.UtcNow,
                    MessageId = request.Metadata.MessageId
                }
            }, x => x.SetRoutingKey("notification-queue"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during fraud check on account {AccountGuid}. Blocking transaction due to uncertainty.", data.Account.Guid);

            await context.Publish(new FraudNotification
            {
                Data = new FraudNotificationData
                {
                    Message = "Transaction blocked: fraud check could not be completed due to an unexpected error."
                },
                Metadata = new FraudNotificationMetadata
                {
                    MessageType = request.Metadata.MessageType,
                    MessageTimestamp = DateTime.UtcNow,
                    MessageId = request.Metadata.MessageId
                }
            }, x => x.SetRoutingKey("notification-queue"));
        }

        logger.LogInformation("Fraud check complete for account {AccountGuid}", data.Account.Guid);
    }
}
