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
        logger.LogInformation("Received fraud check request {Id} for {Amount} {Currency}",
            request.Id, request.Amount, request.Currency);

        // Build the user prompt from transaction fields
        var prompt = $"Sender Account: {request.SenderAccount}\n" +
                     $"Receiver Account: {request.ReceiverAccount}\n" +
                     $"Amount: {request.Amount}\n" +
                     $"Currency: {request.Currency}\n" +
                     $"Origin IP Address: {request.OriginIpAddress}";

        // Send the prompt to OpenRouter AI
        FraudChecked fraudChecked;
        
        try
        {
            var result = await openRouterService.SendPromptAsync(prompt, context.CancellationToken);
            fraudChecked = new FraudChecked
            {
                RequestId = request.Id,
                IsFraud = result.IsFraud,
                Reason = result.Reason,
                RiskScore = result.RiskScore
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OpenRouter request failed for fraud check {Id}. Publishing fallback response.", request.Id);
            fraudChecked = new FraudChecked
            {
                RequestId = request.Id,
                IsFraud = false,
                Reason = "Fraud check could not be completed due to an AI service error.",
                RiskScore = 0
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during fraud check {Id}. Publishing fallback response.", request.Id);
            fraudChecked = new FraudChecked
            {
                RequestId = request.Id,
                IsFraud = false,
                Reason = "Fraud check could not be completed due to an unexpected error.",
                RiskScore = 0
            };
        }

        // Publish response back to bus
        await context.Publish(fraudChecked);

        logger.LogInformation("Published response for request {Id}", request.Id);
    }
}