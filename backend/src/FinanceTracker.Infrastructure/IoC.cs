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
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.AddTransient<GroqAuthHandler>();
        services.AddHttpClient<IGroqService, GroqService>()
                .AddHttpMessageHandler<GroqAuthHandler>();

        // 3. Telegram External Integration
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
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
