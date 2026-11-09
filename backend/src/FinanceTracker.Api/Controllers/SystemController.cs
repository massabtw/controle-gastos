using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.System;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api")]
public class SystemController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "pong", timestamp = DateTime.UtcNow });
    }

    [HttpGet("system/status")]
    public async Task<IActionResult> GetStatus(
        [FromServices] ISystemService service)
    {
        var result = await service.GetStatusAsync();
        return result.Ok();
    }
}
