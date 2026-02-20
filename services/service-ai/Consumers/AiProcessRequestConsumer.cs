using MassTransit;
using service_ai.Messages;

namespace service_ai.Consumers;

public class AiProcessRequestConsumer(ILogger<AiProcessRequestConsumer> logger) : IConsumer<AiProcessRequest>
{
    public async Task Consume(ConsumeContext<AiProcessRequest> context)
    {
        var request = context.Message;
        logger.LogInformation("Received request {Id} with payload: {Payload}", request.Id, request.Payload);

        // Do stuff here
        var result = $"Processed: {request.Payload}";

        // Publish back to bus
        await context.Publish(new AiProcessResponse
        {
            RequestId = request.Id,
            Result = result
        });

        logger.LogInformation("Published response for request {Id}", request.Id);
    }
}