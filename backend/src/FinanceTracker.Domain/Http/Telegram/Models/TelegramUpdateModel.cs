using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Telegram.Models;

public class TelegramUpdateModel
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessageModel? Message { get; set; }
}
