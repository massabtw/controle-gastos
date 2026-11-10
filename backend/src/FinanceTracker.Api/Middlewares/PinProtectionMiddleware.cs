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
        // 1. Always allow OPTIONS pre-flight requests (CORS)
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 2. Allow public bypass endpoints:
        // - Auth endpoints (/api/auth/login, /api/auth/verify)
        // - Telegram Webhook (/api/telegram/*)
        // - iOS Shortcut webhook (/api/integrations/ios-shortcut - protected by SecretPin)
        // - Health / Ping / Root / Swagger
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/api/auth/") ||
            path.StartsWith("/api/telegram") ||
            path.StartsWith("/api/integrations/ios-shortcut") ||
            path.StartsWith("/api/ping") ||
            path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path == "/" ||
            path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        // 3. Protect ALL Dashboard endpoints (GET, POST, PUT, DELETE):
        var token = context.Request.Headers["X-Admin-Token"].FirstOrDefault()
            ?? context.Request.Headers["X-App-Pin"].FirstOrDefault()
            ?? context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();

        if (!AdminTokenHelper.IsValidToken(token, _configuration))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var responseObj = new
            {
                message = "Acesso não autorizado. Autentique-se como administrador para acessar o painel.",
                requiresAuth = true
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
            return;
        }

        await _next(context);
    }
}
