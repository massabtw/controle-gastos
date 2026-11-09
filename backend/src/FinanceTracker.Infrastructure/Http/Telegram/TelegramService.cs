using System.Text;
using System.Text.Json;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Http.Telegram.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Infrastructure.Http.Telegram;

public class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(long chatId, string text, string? parseMode = "Markdown")
    {
        var token = !string.IsNullOrWhiteSpace(_options.BotToken)
            ? _options.BotToken
            : Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
              ?? Environment.GetEnvironmentVariable("TELEGRAM_TOKEN")
              ?? Environment.GetEnvironmentVariable("TELEGRAM__BOTTOKEN")
              ?? Environment.GetEnvironmentVariable("Telegram__BotToken")
              ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Telegram bot token is not configured.");
            return false;
        }

        try
        {
            var payload = new TelegramSendMessageRequestModel
            {
                ChatId = chatId,
                Text = text,
                ParseMode = parseMode
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var url = $"{_options.BaseUrl.TrimEnd('/')}/bot{token}/sendMessage";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to Telegram API: {Error}", err);

                // If markdown formatting failed, fallback to plain text to rescue delivery
                if (!string.IsNullOrWhiteSpace(parseMode))
                {
                    payload.ParseMode = null;
                    var fallbackContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var fallbackResponse = await _httpClient.PostAsync(url, fallbackContent);
                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Telegram message delivered successfully via plain text fallback.");
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending Telegram message to chatId {ChatId}", chatId);
            return false;
        }
    }

    public async Task<bool> SendToAllowedChatAsync(string text, string? parseMode = "Markdown")
    {
        var chatId = _options.AllowedChatId != 0
            ? _options.AllowedChatId
            : (long.TryParse(
                Environment.GetEnvironmentVariable("TELEGRAM__ALLOWEDCHATID")
                ?? Environment.GetEnvironmentVariable("Telegram__AllowedChatId")
                ?? Environment.GetEnvironmentVariable("TELEGRAM_ALLOWED_CHAT_ID"),
                out var envChatId) && envChatId != 0
                ? envChatId
                : 5850175333L);

        return await SendMessageAsync(chatId, text, parseMode);
    }
}
