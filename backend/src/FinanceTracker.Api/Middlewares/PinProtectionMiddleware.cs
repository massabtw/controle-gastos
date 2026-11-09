using System.Text.Json;

namespace FinanceTracker.Api.Middlewares;

public class PinProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PinProtectionMiddleware> _logger;

    public PinProtectionMiddleware(RequestDelegate next, ILogger<PinProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var appPin = Environment.GetEnvironmentVariable("APP_PIN");

        // If no PIN is configured, allow all traffic
        if (string.IsNullOrWhiteSpace(appPin))
        {
            await _next(context);
            return;
        }

        // Allow safe methods
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsOptions(method) || HttpMethods.IsHead(method))
        {
            await _next(context);
            return;
        }

        // Allow public webhooks and ping
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/api/telegram/webhook") ||
            path.StartsWith("/api/ping") ||
            path.StartsWith("/api/integrations/ios-shortcut"))
        {
            await _next(context);
            return;
        }

        // Check X-App-Pin header
        if (!context.Request.Headers.TryGetValue("X-App-Pin", out var providedPin) ||
            !string.Equals(providedPin.ToString().Trim(), appPin.Trim(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var responseObj = new { message = "PIN de acesso inválido ou não informado. Digite o PIN correto nas configurações do app." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
            return;
        }

        await _next(context);
    }
}
