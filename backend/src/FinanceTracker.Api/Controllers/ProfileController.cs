using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileRepository _profileRepository;

    public ProfileController(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _profileRepository.GetProfileAsync();
        var commitments = await _profileRepository.GetFixedCommitmentsAsync(onlyActive: false);

        return Ok(new
        {
            profile,
            commitments
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] FinancialProfile profile)
    {
        var success = await _profileRepository.UpdateProfileAsync(profile);
        if (!success)
            return BadRequest(new { message = "Could not update profile." });

        return Ok(new { message = "Profile updated successfully.", profile });
    }

    [HttpPost("commitments")]
    public async Task<IActionResult> AddCommitment([FromBody] FixedCommitment commitment)
    {
        if (string.IsNullOrWhiteSpace(commitment.Title))
            return BadRequest(new { message = "Title is required." });

        if (commitment.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        if (string.IsNullOrWhiteSpace(commitment.StartCycle))
            return BadRequest(new { message = "StartCycle (YYYY-MM) is required." });

        var id = await _profileRepository.AddFixedCommitmentAsync(commitment);
        commitment.Id = id;

        return StatusCode(201, commitment);
    }

    [HttpPut("commitments/{id:int}")]
    public async Task<IActionResult> UpdateCommitment(int id, [FromBody] FixedCommitment commitment)
    {
        commitment.Id = id;
        var success = await _profileRepository.UpdateFixedCommitmentAsync(commitment);
        if (!success)
            return NotFound(new { message = "Commitment not found." });

        return Ok(new { message = "Commitment updated successfully." });
    }

    [HttpDelete("commitments/{id:int}")]
    public async Task<IActionResult> DeleteCommitment(int id)
    {
        var success = await _profileRepository.DeleteFixedCommitmentAsync(id);
        if (!success)
            return NotFound(new { message = "Commitment not found." });

        return Ok(new { message = "Commitment deleted successfully." });
    }
}
