using FinanceTracker.Api.BackgroundServices;
using FinanceTracker.Api.Middlewares;
using FinanceTracker.Application;
using FinanceTracker.Infrastructure;
using FinanceTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Render / Docker if PORT env var is present
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Controllers & JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean DI Registration via layer IoC
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Background Services
builder.Services.AddHttpClient();
builder.Services.AddHostedService<TelegramPollingBackgroundService>();

// CORS configuration (Allows Vercel domains, localhost, and all preview URLs)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Security: PIN Protection for writes if APP_PIN is set
app.UseMiddleware<PinProtectionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Database Migration on startup (PostgreSQL Neon auto-init or SQL Server)
try
{
    using var scope = app.Services.CreateScope();
    var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
    await migrator.MigrateAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not run database migrations.");
}

// Auto-register Telegram Webhook if deployed on Render or with public URL
var publicBaseUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL")
    ?? Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");

if (!string.IsNullOrWhiteSpace(publicBaseUrl))
{
    try
    {
        var botToken = builder.Configuration["Telegram:BotToken"];
        if (!string.IsNullOrWhiteSpace(botToken))
        {
            using var http = new HttpClient();
            var webhookUrl = $"{publicBaseUrl.TrimEnd('/')}/api/telegram/webhook";
            var setWebhookEndpoint = $"https://api.telegram.org/bot{botToken}/setWebhook?url={Uri.EscapeDataString(webhookUrl)}";
            var resp = await http.PostAsync(setWebhookEndpoint, null);
            app.Logger.LogInformation("Telegram Webhook auto-registration to {WebhookUrl}: {Status}", webhookUrl, resp.StatusCode);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to auto-register Telegram webhook.");
    }
}

app.Run();
