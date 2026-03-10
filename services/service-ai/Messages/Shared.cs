namespace service_ai.Messages;

public record AccountInfo
{
    public string Guid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
