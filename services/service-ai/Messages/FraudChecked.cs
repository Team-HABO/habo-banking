namespace service_ai.Messages;

public record FraudChecked
{
    public Guid RequestId { get; init; }
    public bool IsFraud { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double RiskScore { get; init; }
}