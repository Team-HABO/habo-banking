using System.Text.Json.Serialization;

namespace service_ai.Models;

public class OpenRouterRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = [];
}

public class ChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

public class OpenRouterResponse
{
    [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
}

