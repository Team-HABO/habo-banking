using System.Text.Json;
using service_currency_exchange.Models;

namespace service_currency_exchange.Services;

public interface ICurrencyService
{
    /// <summary>
    /// Fetches the exchange rate from DKK to <paramref name="targetCurrency"/>.
    /// Returns null if the currency is unsupported or the API call fails.
    /// </summary>
    Task<decimal?> GetRateFromDkkAsync(string targetCurrency, CancellationToken cancellationToken = default);
}

public class CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger) : ICurrencyService
{
    public async Task<decimal?> GetRateFromDkkAsync(string targetCurrency, CancellationToken cancellationToken = default)
    {
        var url = $"latest?base=DKK&symbols={Uri.EscapeDataString(targetCurrency.ToUpperInvariant())}";
        logger.LogInformation("Fetching exchange rate DKK → {TargetCurrency} from Frankfurter", targetCurrency);

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Frankfurter API error {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }

            var result = JsonSerializer.Deserialize<FrankfurterResponse>(body);
            var normalised = targetCurrency.ToUpperInvariant();

            var rate = result?.Rates.GetValueOrDefault(normalised);

            if (rate is not null)
            {
                logger.LogInformation("Exchange rate DKK → {TargetCurrency} = {Rate}", normalised, rate);
                return rate;
            }

            logger.LogWarning("Currency {TargetCurrency} not found in Frankfurter response: {Body}", targetCurrency, body);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching exchange rate for {TargetCurrency}", targetCurrency);
            return null;
        }
    }
}

