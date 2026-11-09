using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Groq.Models;

public class GroqChatResponseModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<GroqChoiceModel> Choices { get; set; } = new();
}
