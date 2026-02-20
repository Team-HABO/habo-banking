using MassTransit;
using service_ai.Messages;
using service_ai.Services;

namespace service_ai.Consumers;

public class AiProcessRequestConsumer(
    ILogger<AiProcessRequestConsumer> logger,
    IOpenRouterService openRouterService) : IConsumer<AiProcessRequest>
{
    public async Task Consume(ConsumeContext<AiProcessRequest> context)
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
        var result =
            await openRouterService.SendPromptAsync(prompt, context.CancellationToken);

        // Publish response back to bus
        await context.Publish(new AiProcessResponse
        {
            RequestId = request.Id,
            IsFraud = result.IsFraud,
            Reason = result.Reason,
            RiskScore = result.RiskScore
        });

        logger.LogInformation("Published response for request {Id}", request.Id);
    }
}