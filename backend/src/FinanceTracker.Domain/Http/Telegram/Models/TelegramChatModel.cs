using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Telegram.Models;

public class TelegramChatModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
