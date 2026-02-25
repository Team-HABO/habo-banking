using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using service_ai.Models;

namespace service_ai.Services;

public interface IOpenRouterService
{
    Task<FraudCheckResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default);
}

public class OpenRouterService : IOpenRouterService
{
    private readonly string _defaultModel;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterService> _logger;

    public OpenRouterService(HttpClient httpClient, ILogger<OpenRouterService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = configuration["OpenRouter:ApiKey"]
                     ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured.");

        _defaultModel = configuration["OpenRouter:Model"] ??
                        throw new InvalidOperationException("OpenRouter:Model is not configured.");

        _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Title", "habo-banking-ai");
    }

    public async Task<FraudCheckResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new OpenRouterRequest
        {
            Model = _defaultModel,
            Messages =
            [
                new ChatMessage
                {
                    Role = "system",
                    Content = Prompts.FraudDetection
                },
                new ChatMessage { Role = "user", Content = prompt }
            ]
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending prompt to OpenRouter (model: {Model})", requestBody.Model);

        using var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenRouter API error {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"OpenRouter API returned {response.StatusCode}: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseBody);
        var message = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

        _logger.LogInformation("Received response from OpenRouter ({Length} chars)", message.Length);

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("AI returned an empty response");
            return new FraudCheckResult();
        }

        FraudCheckResult? fraudResult;
        try
        {
            fraudResult = JsonSerializer.Deserialize<FraudCheckResult>(message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize AI response as FraudCheckResult: {Content}", message);
            fraudResult = null;
        }

        if (fraudResult is null)
        {
            _logger.LogWarning("AI response deserialized to null: {Content}", message);
            return new FraudCheckResult();
        }

        if (string.IsNullOrWhiteSpace(fraudResult.Reason))
        {
            _logger.LogWarning("FraudCheckResult has an empty Reason. Raw response: {Content}", message);
        }

        if (fraudResult.RiskScore is < 0.0 or > 1.0)
        {
            _logger.LogWarning("FraudCheckResult has an out-of-range RiskScore ({RiskScore}). Raw response: {Content}", fraudResult.RiskScore, message);
        }

        return fraudResult;

    }
}

