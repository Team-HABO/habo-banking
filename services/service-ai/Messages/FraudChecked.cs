namespace service_ai.Messages;

public record FraudChecked
{
    public string AccountGuid { get; init; } = string.Empty;
    public bool IsFraud { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double RiskScore { get; init; }
    public string MessageType { get; init; } = string.Empty;
}
