using System.Text.Json.Serialization;

namespace service_currency_exchange.Models;

/// <summary>
/// Response from https://api.frankfurter.dev/v1/latest?base=DKK&amp;symbols={currency}
/// </summary>
public class FrankfurterResponse
{
    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = [];
}

