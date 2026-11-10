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
        // 1. Allow safe read-only HTTP methods (GET, HEAD, OPTIONS) publicly
        // This enables Guest Mode to load Dashboard, Expenses, Invoices in read-only mode
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 2. Allow public bypass endpoints for state changes (POST/PUT/DELETE):
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

        // 3. Protect state-changing requests (POST, PUT, PATCH, DELETE):
        var token = context.Request.Headers["X-Admin-Token"].FirstOrDefault()
            ?? context.Request.Headers["X-App-Pin"].FirstOrDefault()
            ?? context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();

        if (!AdminTokenHelper.IsValidToken(token, _configuration))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var responseObj = new
            {
                message = "Ação restrita. Autentique-se como administrador para realizar alterações.",
                requiresAuth = true
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
            return;
        }

        await _next(context);
    }
}
