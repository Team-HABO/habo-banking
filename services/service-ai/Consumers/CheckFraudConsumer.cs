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
        FraudChecked fraudChecked;
        
        try
        {
            var result = await openRouterService.SendPromptAsync(prompt, context.CancellationToken);
            fraudChecked = new FraudChecked
            {
                AccountGuid = data.Account.Guid,
                IsFraud = result.IsFraud,
                Reason = result.Reason,
                RiskScore = result.RiskScore,
                MessageType = request.Metadata.MessageType
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OpenRouter request failed for fraud check on account {AccountGuid}. Publishing fallback response.", data.Account.Guid);
            fraudChecked = new FraudChecked
            {
                AccountGuid = data.Account.Guid,
                IsFraud = false,
                Reason = "Fraud check could not be completed due to an AI service error.",
                RiskScore = 0,
                MessageType = request.Metadata.MessageType
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during fraud check on account {AccountGuid}. Publishing fallback response.", data.Account.Guid);
            fraudChecked = new FraudChecked
            {
                AccountGuid = data.Account.Guid,
                IsFraud = false,
                Reason = "Fraud check could not be completed due to an unexpected error.",
                RiskScore = 0,
                MessageType = request.Metadata.MessageType
            };
        }

        // Publish response back to bus
        await context.Publish(fraudChecked);

        logger.LogInformation("Published fraud check response for account {AccountGuid}", request.Data.Account.Guid);
    }
}