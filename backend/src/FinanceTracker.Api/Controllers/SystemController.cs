using FinanceTracker.Api.Domain.Services;
using FinanceTracker.Api.Infrastructure.Data;
using FinanceTracker.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProfileRepository _profileRepository;

    public SystemController(IDbConnectionFactory connectionFactory, IProfileRepository profileRepository)
    {
        _connectionFactory = connectionFactory;
        _profileRepository = profileRepository;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var dbConnected = false;
        string? dbError = null;

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            dbConnected = true;
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        var profile = await _profileRepository.GetProfileAsync();
        var currentCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
        var (closingDate, dueDate) = BillingCycleCalculator.GetCycleDates(currentCycle, profile.ClosingDay, profile.DueDay);

        return Ok(new
        {
            status = dbConnected ? "ONLINE" : "DEGRADED",
            database = new { connected = dbConnected, error = dbError },
            activeBillingCycle = currentCycle,
            cycleClosingDate = closingDate,
            cycleDueDate = dueDate,
            serverTime = DateTime.Now
        });
    }
}
