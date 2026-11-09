using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Telegram.Models;

public class TelegramMessageModel
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChatModel Chat { get; set; } = new();

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }
}
