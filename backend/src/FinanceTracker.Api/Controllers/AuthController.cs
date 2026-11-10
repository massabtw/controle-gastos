using FinanceTracker.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record LoginRequest(string? Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (AdminTokenHelper.IsValidPassword(request?.Password, _configuration))
        {
            var expectedPassword = AdminTokenHelper.GetAdminPassword(_configuration);
            var token = AdminTokenHelper.GenerateToken(expectedPassword);

            return Ok(new
            {
                success = true,
                token,
                expiresInDays = 90,
                message = "Login de administrador realizado com sucesso."
            });
        }

        return StatusCode(StatusCodes.Status401Unauthorized, new
        {
            success = false,
            message = "Senha de administrador incorreta."
        });
    }

    [HttpGet("verify")]
    public IActionResult Verify()
    {
        var token = Request.Headers["X-Admin-Token"].FirstOrDefault()
            ?? Request.Headers["X-App-Pin"].FirstOrDefault()
            ?? Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();

        if (AdminTokenHelper.IsValidToken(token, _configuration))
        {
            return Ok(new
            {
                valid = true,
                message = "Token de administrador válido."
            });
        }

        return StatusCode(StatusCodes.Status401Unauthorized, new
        {
            valid = false,
            message = "Não autorizado. Token de administrador inválido ou não fornecido."
        });
    }
}
