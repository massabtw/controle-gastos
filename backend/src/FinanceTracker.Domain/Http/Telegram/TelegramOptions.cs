namespace FinanceTracker.Domain.Http.Telegram;

public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public long AllowedChatId { get; set; }
    public string BaseUrl { get; set; } = "https://api.telegram.org";
}
