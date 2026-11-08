using FinanceTracker.Api.Domain.Services;
using FinanceTracker.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommitmentsController : ControllerBase
{
    private readonly IProfileRepository _profileRepository;

    public CommitmentsController(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    [HttpPost("{id:int}/pay-from-caixinha")]
    public async Task<IActionResult> PayFromCaixinha(int id, [FromQuery] string? cycle)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var targetCycle = string.IsNullOrWhiteSpace(cycle)
            ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
            : cycle;

        var success = await _profileRepository.MarkCommitmentPaidFromCaixinhaAsync(id, targetCycle);
        if (!success)
            return BadRequest(new { message = "Could not mark commitment as paid from Caixinha." });

        return Ok(new { message = "Commitment marked as paid from Caixinha.", cycle = targetCycle });
    }
}
