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

    [HttpPost("{id:int}/toggle-paid")]
    public async Task<IActionResult> TogglePaid(int id, [FromQuery] string? cycle)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var targetCycle = string.IsNullOrWhiteSpace(cycle)
            ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
            : cycle;

        var (success, isPaid, balanceAfter) = await _profileRepository.ToggleCommitmentPaidAsync(id, targetCycle);
        if (!success)
            return BadRequest(new { message = "Could not toggle commitment paid state." });

        return Ok(new
        {
            message = isPaid ? "Commitment marked as paid." : "Commitment unmarked as paid and refunded.",
            isPaid,
            cycle = targetCycle,
            balanceAfter
        });
    }

    [HttpPost("{id:int}/pay-from-caixinha")]
    public async Task<IActionResult> PayFromCaixinha(int id, [FromQuery] string? cycle)
    {
        return await TogglePaid(id, cycle);
    }
}
