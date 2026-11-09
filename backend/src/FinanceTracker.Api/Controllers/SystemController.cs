using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.System;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromServices] ISystemService service)
    {
        var result = await service.GetStatusAsync();
        return result.Ok();
    }
}
