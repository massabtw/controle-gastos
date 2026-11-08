using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulatorController : ControllerBase
{
    private readonly IFinancialEngineService _financialEngineService;

    public SimulatorController(IFinancialEngineService financialEngineService)
    {
        _financialEngineService = financialEngineService;
    }

    [HttpPost]
    public async Task<IActionResult> Simulate([FromBody] SimulatorRequestDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        if (dto.Installments <= 0)
            dto.Installments = 1;

        var result = await _financialEngineService.SimulatePurchaseAsync(dto);
        return Ok(result);
    }
}
