using System.Text.Json.Serialization;

namespace FinanceTracker.Domain.Http.Telegram.Models;

public class TelegramSendMessageResponseModel
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public TelegramMessageModel? Result { get; set; }
}
