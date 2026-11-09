using System.Text.Json;
using FinanceTracker.Application.Interfaces.Commands;
using FinanceTracker.Domain.Http.Telegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Api.BackgroundServices;

public class TelegramPollingBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ITelegramService _telegramService;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;

    public TelegramPollingBackgroundService(
        IConfiguration configuration,
        ITelegramService telegramService,
        IServiceProvider serviceProvider,
        HttpClient httpClient,
        ILogger<TelegramPollingBackgroundService> logger)
    {
        _configuration = configuration;
        _telegramService = telegramService;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var publicUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL")
            ?? Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");

        if (!string.IsNullOrWhiteSpace(publicUrl))
        {
            _logger.LogInformation("Public URL detected ({PublicUrl}). Telegram Webhook mode is active; polling service will stay idle.", publicUrl);
            return;
        }

        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogInformation("Telegram BotToken not configured. Polling background service is inactive.");
            return;
        }

        _logger.LogInformation("Starting Telegram Long Polling service...");

        try
        {
            var deleteWebhookUrl = $"https://api.telegram.org/bot{botToken}/deleteWebhook";
            await _httpClient.PostAsync(deleteWebhookUrl, null, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call deleteWebhook on Telegram.");
        }

        var allowedChatId = _configuration.GetValue<long>("Telegram:AllowedChatId");
        if (allowedChatId != 0)
        {
            try
            {
                await _telegramService.SendMessageAsync(allowedChatId, "🚀 FinanceTracker Online! IA Groq + Bot conectados. Mande um gasto (ex: 'uber 25', 'pix 30 corte') ou '/status'.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send startup ping to Telegram.");
            }
        }

        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{botToken}/getUpdates?offset={offset}&timeout=25";
                var response = await _httpClient.GetAsync(url, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(stoppingToken);
                    using var doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("result", out var results) && results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var update in results.EnumerateArray())
                        {
                            if (update.TryGetProperty("update_id", out var updateIdElem))
                            {
                                offset = updateIdElem.GetInt64() + 1;
                            }

                            if (update.TryGetProperty("message", out var msgElem) &&
                                msgElem.TryGetProperty("text", out var textElem) &&
                                msgElem.TryGetProperty("chat", out var chatElem) &&
                                chatElem.TryGetProperty("id", out var chatIdElem))
                            {
                                var text = textElem.GetString();
                                var chatId = chatIdElem.GetInt64();

                                if (string.IsNullOrWhiteSpace(text)) continue;

                                if (allowedChatId != 0 && chatId != allowedChatId)
                                {
                                    _logger.LogWarning("Unauthorized Telegram ChatId: {ChatId}", chatId);
                                    continue;
                                }

                                if (text.Trim().Equals("/start", StringComparison.OrdinalIgnoreCase))
                                {
                                    var welcome = "👋 Olá! Sou o seu assistente financeiro do BurnTracker.\n" +
                                                  "Aqui você controla seus gastos do cartão e saídas da caixinha em tempo real!\n\n" +
                                                  "Comandos e exemplos que você pode mandar:\n" +
                                                  "• Gasto Cartão: 'uber 25', 'almoço 34,90', 'teclado 300 3x'\n" +
                                                  "• Pix Caixinha: 'pix 40 corte', 'paguei 25 no pix lanche'\n" +
                                                  "• Receber Pix: 'recebi 50 pix do joao'\n" +
                                                  "• Adicionar Caixinha: '+10 caixinha'\n" +
                                                  "• Ajustar Fatura: 'fatura atual 350'\n" +
                                                  "• Liquidar Fatura: 'paguei a fatura' ou '/pagarfatura'\n" +
                                                  "• Reinvestir Sobra: 'investir sobra'\n" +
                                                  "• Atualizar Caixinha: 'saldo caixinha 4500'\n" +
                                                  "• Simulação: '/simular 300 3x'\n" +
                                                  "• Resumo do Ciclo: '/status'\n" +
                                                  "• Desfazer Último: '/desfazer'";

                                    await _telegramService.SendMessageAsync(chatId, welcome);
                                    continue;
                                }

                                using var scope = _serviceProvider.CreateScope();
                                var cmdProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessorService>();
                                var cmdResult = await cmdProcessor.ProcessCommandAsync(text);
                                if (cmdResult.IsSuccess && cmdResult.Value != null)
                                {
                                    await _telegramService.SendMessageAsync(chatId, cmdResult.Value.Reply);
                                }
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Telegram polling loop.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
