using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Telegram.Models;

public class TelegramSendMessageRequestModel
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("parse_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParseMode { get; set; }
}
