using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Api.Services.Telegram;

public class TelegramPollingBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;

    public TelegramPollingBackgroundService(
        IConfiguration configuration,
        ILogger<TelegramPollingBackgroundService> logger,
        IServiceProvider serviceProvider,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogInformation("Telegram BotToken not configured. Polling background service is inactive.");
            return;
        }

        _logger.LogInformation("Starting Telegram Long Polling service...");
        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{botToken}/getUpdates?offset={offset}&timeout=20";
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

                            using var scope = _serviceProvider.CreateScope();
                            var botService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
                            await botService.HandleUpdateAsync(update);
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
