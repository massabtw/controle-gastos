using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Api.Services.Telegram;

public interface ITelegramBotService
{
    Task HandleUpdateAsync(JsonElement update);
}

public class TelegramBotService : ITelegramBotService
{
    private readonly HttpClient _httpClient;
    private readonly ICommandProcessorService _commandProcessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        HttpClient httpClient,
        ICommandProcessorService commandProcessor,
        IConfiguration configuration,
        ILogger<TelegramBotService> logger)
    {
        _httpClient = httpClient;
        _commandProcessor = commandProcessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(JsonElement update)
    {
        try
        {
            if (!update.TryGetProperty("message", out var messageElement))
            {
                return;
            }

            if (!messageElement.TryGetProperty("text", out var textElement) ||
                !messageElement.TryGetProperty("chat", out var chatElement) ||
                !chatElement.TryGetProperty("id", out var chatIdElement))
            {
                return;
            }

            var text = textElement.GetString();
            var chatId = chatIdElement.GetInt64();

            if (string.IsNullOrWhiteSpace(text))
                return;

            // Validate AllowedChatId if configured
            var configuredAllowedChatId = _configuration.GetValue<long>("Telegram:AllowedChatId");
            if (configuredAllowedChatId != 0 && chatId != configuredAllowedChatId)
            {
                _logger.LogWarning("Unauthorized Telegram ChatId: {ChatId}. Configured: {AllowedChatId}", chatId, configuredAllowedChatId);
                return;
            }

            // Handle /start command
            if (text.Trim().Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                var welcome = "👋 Olá! Sou o seu assistente financeiro do BurnTracker.\n" +
                              "Aqui você controla seus gastos do cartão e saídas da caixinha em tempo real!\n\n" +
                              "Comandos e exemplos que você pode mandar:\n" +
                              "• Gasto Cartão: 'uber 25', 'almoço 34,90', 'teclado 300 3x'\n" +
                              "• Pix Caixinha: 'pix 40 corte', 'paguei 25 no pix lanche'\n" +
                              "• Ajustar Fatura: 'fatura atual 350'\n" +
                              "• Atualizar Caixinha: 'saldo caixinha 4500'\n" +
                              "• Simulação: '/simular 300 3x' ou 'simular 300 em 3x'\n" +
                              "• Resumo do Ciclo: '/status' ou 'como ta minha fatura'\n" +
                              "• Desfazer Último: '/desfazer' ou 'apagar ultimo'";

                await SendTelegramMessageAsync(chatId, welcome);
                return;
            }

            var result = await _commandProcessor.ProcessCommandAsync(text);
            await SendTelegramMessageAsync(chatId, result.Reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram update");
        }
    }

    private async Task SendTelegramMessageAsync(long chatId, string text)
    {
        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken)) return;

        var payload = new
        {
            chat_id = chatId,
            text = text
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var sendUrl = $"https://api.telegram.org/bot{botToken}/sendMessage";
        var response = await _httpClient.PostAsync(sendUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send message to Telegram API: {Error}", err);
        }
    }
}
