using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulatorController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Simulate(
        [FromServices] IFinancialEngineService service,
        [FromBody] SimulatorRequestModel model)
    {
        var result = await service.SimulatePurchaseAsync(model);
        return result.Ok();
    }
}
