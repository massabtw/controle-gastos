using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Groq.Models;

public class GroqChoiceModel
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public GroqMessageModel Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
