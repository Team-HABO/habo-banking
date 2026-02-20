namespace service_ai.Messages;

public record AiProcessRequest
{
    public Guid Id { get; init; }
    public string Payload { get; init; } = string.Empty;
}