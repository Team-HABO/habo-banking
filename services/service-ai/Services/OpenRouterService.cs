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
                    Content =
                        "### Role\nYou are a specialized Fraud Detection Security Layer for a banking microservice. Your task is to analyze transaction metadata and identify high-risk activity based on specific organizational heuristics.\n\n### Input Variables\n- Sender Account\n- Receiver Account\n- Amount\n- Currency\n- Origin IP Address\n\n### Risk Heuristics\n1. **Threshold Violation:** Flag any transaction where the Amount is greater than 10,000 (regardless of currency).\n2. **Geographical Risk:** Flag any transaction where the Origin IP Address is identified as originating from India. \n\n### Output Format\nYou must return a valid JSON object with the following keys:\n- \"is_fraud\": boolean (true if any heuristic is triggered, false otherwise)\n- \"reason\": string (A brief explanation of which heuristic was triggered, or \"Clear\" if false)\n- \"risk_score\": float (0.0 to 1.0)\n\n### Constraint\nDo not include any conversational text or markdown formatting outside of the JSON block."
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

        var fraudResult = JsonSerializer.Deserialize<FraudCheckResult>(message);
        
        if (fraudResult is not null)
        {
            return fraudResult;
        }

        _logger.LogError("Failed to parse AI response as FraudCheckResult: {Content}", message);
        throw new JsonException($"Failed to parse AI response as FraudCheckResult: {message}");

    }
}

