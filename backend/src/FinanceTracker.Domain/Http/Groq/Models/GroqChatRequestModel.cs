using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Groq.Models;

public class GroqChatRequestModel
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<GroqMessageModel> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.1;
}
