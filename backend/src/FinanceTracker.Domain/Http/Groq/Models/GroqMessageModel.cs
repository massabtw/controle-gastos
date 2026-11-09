using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Groq.Models;

public class GroqMessageModel
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
