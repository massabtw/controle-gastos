namespace FinanceTracker.Domain.Http.Telegram;

public interface ITelegramService
{
    Task<bool> SendMessageAsync(long chatId, string text);
}
