namespace FinanceTracker.Domain.Http.Telegram;

public interface ITelegramService
{
    Task<bool> SendMessageAsync(long chatId, string text, string? parseMode = "Markdown");
    Task<bool> SendToAllowedChatAsync(string text, string? parseMode = "Markdown");
}
