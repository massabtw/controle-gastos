using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IFinancialEngineService _financialEngineService;

    public DashboardController(IFinancialEngineService financialEngineService)
    {
        _financialEngineService = financialEngineService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] string? cycle)
    {
        var summary = await _financialEngineService.GetDashboardSummaryAsync(cycle);
        return Ok(summary);
    }

    [HttpGet("projections")]
    public async Task<IActionResult> GetProjections([FromQuery] int months = 6)
    {
        var clampedMonths = Math.Clamp(months, 1, 24);
        var projections = await _financialEngineService.GetProjectionsAsync(clampedMonths);
        return Ok(projections);
    }
}
