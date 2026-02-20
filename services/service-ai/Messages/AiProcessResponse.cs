namespace service_ai.Messages;

public record AiProcessResponse
{
    public Guid RequestId { get; init; }
    public string Result { get; init; } = string.Empty;
}