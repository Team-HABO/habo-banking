using System.Text.Json.Serialization;

namespace service_ai.Models;

public record FraudCheckResult
{
    [JsonPropertyName("is_fraud")]
    public bool IsFraud { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("risk_score")]
    public double RiskScore { get; init; }
}

