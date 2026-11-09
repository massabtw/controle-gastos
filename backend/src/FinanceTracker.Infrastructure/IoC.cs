using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.UnitOfWorks;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Http.Groq;
using FinanceTracker.Infrastructure.Http.Telegram;
using FinanceTracker.Infrastructure.UnitOfWorks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Infrastructure;

public static class IoC
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Unit of Work (Always AddTransient)
        services.AddTransient<IUnitOfWorkApp, UnitOfWorkApp>();

        // 2. Groq External Integration
        services.Configure<GroqOptions>(options =>
        {
            configuration.GetSection("Groq").Bind(options);
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                    ?? Environment.GetEnvironmentVariable("GROQ__APIKEY")
                    ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
                    ?? string.Empty;
            }
        });
        services.AddTransient<GroqAuthHandler>();
        services.AddHttpClient<IGroqService, GroqService>()
                .AddHttpMessageHandler<GroqAuthHandler>();

        // 3. Telegram External Integration
        services.Configure<TelegramOptions>(options =>
        {
            configuration.GetSection("Telegram").Bind(options);
            if (string.IsNullOrWhiteSpace(options.BotToken))
            {
                options.BotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
                    ?? Environment.GetEnvironmentVariable("TELEGRAM_TOKEN")
                    ?? Environment.GetEnvironmentVariable("TELEGRAM__BOTTOKEN")
                    ?? Environment.GetEnvironmentVariable("Telegram__BotToken")
                    ?? string.Empty;
            }
            if (options.AllowedChatId == 0)
            {
                var envChat = Environment.GetEnvironmentVariable("TELEGRAM__ALLOWEDCHATID")
                    ?? Environment.GetEnvironmentVariable("Telegram__AllowedChatId")
                    ?? Environment.GetEnvironmentVariable("TELEGRAM_ALLOWED_CHAT_ID");
                if (long.TryParse(envChat, out var cid) && cid != 0)
                {
                    options.AllowedChatId = cid;
                }
                else
                {
                    options.AllowedChatId = 5850175333L;
                }
            }
        });
        services.AddHttpClient<ITelegramService, TelegramService>();

        // 4. Migrations
        var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("FinanceTrackerDb")
            ?? configuration.GetConnectionString("AppDb")
            ?? string.Empty;

        services.AddSingleton(sp => new DatabaseMigrator(connStr, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseMigrator>>()));

        return services;
    }
}
