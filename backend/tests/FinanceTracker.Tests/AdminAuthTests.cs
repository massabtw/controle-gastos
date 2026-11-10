using FinanceTracker.Api.Controllers;
using FinanceTracker.Api.Middlewares;
using FinanceTracker.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceTracker.Tests;

public class AdminAuthTests
{
    private readonly IConfiguration _config;

    public AdminAuthTests()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "Admin:Password", "secret_pass_123" }
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
    }

    [Fact]
    public void GenerateToken_ShouldBeDeterministic()
    {
        var token1 = AdminTokenHelper.GenerateToken("secret_pass_123");
        var token2 = AdminTokenHelper.GenerateToken("secret_pass_123");

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.Equal(token1, token2);
    }

    [Fact]
    public void IsValidPassword_ShouldMatchConfiguredPassword()
    {
        Assert.True(AdminTokenHelper.IsValidPassword("secret_pass_123", _config));
        Assert.False(AdminTokenHelper.IsValidPassword("wrong_pass", _config));
        Assert.False(AdminTokenHelper.IsValidPassword("", _config));
        Assert.False(AdminTokenHelper.IsValidPassword(null, _config));
    }

    [Fact]
    public void IsValidToken_ShouldValidateTokenAndRawPassword()
    {
        var token = AdminTokenHelper.GenerateToken("secret_pass_123");

        Assert.True(AdminTokenHelper.IsValidToken(token, _config));
        Assert.True(AdminTokenHelper.IsValidToken("secret_pass_123", _config)); // Allows raw password
        Assert.False(AdminTokenHelper.IsValidToken("invalid_token", _config));
        Assert.False(AdminTokenHelper.IsValidToken(null, _config));
    }

    [Fact]
    public void AuthController_Login_WithCorrectPassword_Returns200WithToken()
    {
        var controller = new AuthController(_config);
        var result = controller.Login(new AuthController.LoginRequest("secret_pass_123")) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var tokenProp = result.Value!.GetType().GetProperty("token");
        var successProp = result.Value!.GetType().GetProperty("success");

        var token = tokenProp?.GetValue(result.Value)?.ToString();
        var success = (bool)(successProp?.GetValue(result.Value) ?? false);

        Assert.True(success);
        Assert.Equal(AdminTokenHelper.GenerateToken("secret_pass_123"), token);
    }

    [Fact]
    public void AuthController_Login_WithWrongPassword_Returns401()
    {
        var controller = new AuthController(_config);
        var result = controller.Login(new AuthController.LoginRequest("wrong")) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public void GetAdminPassword_WhenNoConfig_DefaultsToUchiha()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var defaultPin = AdminTokenHelper.GetAdminPassword(emptyConfig);
        Assert.Equal("uchiha", defaultPin);
        Assert.True(AdminTokenHelper.IsValidPassword("uchiha", _config));
        Assert.True(AdminTokenHelper.IsValidToken("uchiha", _config));
    }

    [Fact]
    public async Task PinProtectionMiddleware_AllowsGetRequestsPublicly()
    {
        bool nextInvoked = false;
        var middleware = new PinProtectionMiddleware(
            ctx => { nextInvoked = true; return Task.CompletedTask; },
            _config,
            NullLogger<PinProtectionMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/dashboard/summary";

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task PinProtectionMiddleware_BlocksPostWithoutToken()
    {
        bool nextInvoked = false;
        var middleware = new PinProtectionMiddleware(
            ctx => { nextInvoked = true; return Task.CompletedTask; },
            _config,
            NullLogger<PinProtectionMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/expenses";

        await middleware.InvokeAsync(context);

        Assert.False(nextInvoked);
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task PinProtectionMiddleware_AllowsPostWithValidAdminToken()
    {
        bool nextInvoked = false;
        var middleware = new PinProtectionMiddleware(
            ctx => { nextInvoked = true; return Task.CompletedTask; },
            _config,
            NullLogger<PinProtectionMiddleware>.Instance
        );

        var token = AdminTokenHelper.GenerateToken("secret_pass_123");
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/expenses";
        context.Request.Headers["X-Admin-Token"] = token;

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
    }

    [Fact]
    public async Task PinProtectionMiddleware_AllowsPublicBypassEndpoints()
    {
        var endpoints = new[]
        {
            "/api/auth/login",
            "/api/integrations/ios-shortcut",
            "/api/telegram/webhook",
            "/api/ping",
            "/health"
        };

        foreach (var path in endpoints)
        {
            bool nextInvoked = false;
            var middleware = new PinProtectionMiddleware(
                ctx => { nextInvoked = true; return Task.CompletedTask; },
                _config,
                NullLogger<PinProtectionMiddleware>.Instance
            );

            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = path;

            await middleware.InvokeAsync(context);

            Assert.True(nextInvoked, $"Failed for {path}");
        }
    }
}
