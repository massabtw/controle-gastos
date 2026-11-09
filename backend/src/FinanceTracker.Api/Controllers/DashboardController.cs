using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(
        [FromServices] IFinancialEngineService service,
        [FromQuery] string? cycle = null)
    {
        var result = await service.GetDashboardSummaryAsync(cycle);
        return result.Ok();
    }

    [HttpGet("projections")]
    public async Task<IActionResult> GetProjections(
        [FromServices] IFinancialEngineService service,
        [FromQuery] int months = 6)
    {
        var result = await service.GetProjectionsAsync(months);
        return result.Ok();
    }
}
