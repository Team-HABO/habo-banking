namespace service_ai.Messages;

public record AiProcessRequest
{
    public Guid Id { get; init; }
    public string SenderAccount { get; init; } = string.Empty;
    public string ReceiverAccount { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string OriginIpAddress { get; init; } = string.Empty;
}