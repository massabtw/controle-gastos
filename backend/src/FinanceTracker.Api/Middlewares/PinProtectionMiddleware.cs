using System.Text.Json;
using FinanceTracker.Api.Security;

namespace FinanceTracker.Api.Middlewares;

public class PinProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinProtectionMiddleware> _logger;

    public PinProtectionMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<PinProtectionMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Allow safe read-only methods for public dashboard viewing
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsOptions(method) || HttpMethods.IsHead(method))
        {
            await _next(context);
            return;
        }

        // 2. Allow public write endpoints:
        // - Auth login
        // - Telegram Webhook
        // - iOS Shortcut webhook (protected by its own SecretPin)
        // - Health / Ping
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/api/auth/login") ||
            path.StartsWith("/api/telegram") ||
            path.StartsWith("/api/integrations/ios-shortcut") ||
            path.StartsWith("/api/ping") ||
            path.StartsWith("/health") ||
            path == "/")
        {
            await _next(context);
            return;
        }

        // 3. For all other state-changing requests (POST, PUT, DELETE, PATCH):
        var token = context.Request.Headers["X-Admin-Token"].FirstOrDefault()
            ?? context.Request.Headers["X-App-Pin"].FirstOrDefault()
            ?? context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();

        if (!AdminTokenHelper.IsValidToken(token, _configuration))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var responseObj = new
            {
                message = "Acesso não autorizado. Autentique-se como administrador para realizar alterações.",
                requiresAuth = true
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
            return;
        }

        await _next(context);
    }
}
