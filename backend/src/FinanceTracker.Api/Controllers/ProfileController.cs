using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Commitments;
using FinanceTracker.Application.Interfaces.Profile;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(
        [FromServices] IFinancialProfileService service)
    {
        var result = await service.GetProfileAsync();
        return result.Ok();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromServices] IFinancialProfileService service,
        [FromBody] UpdateFinancialProfileModel model)
    {
        var result = await service.UpdateProfileAsync(model);
        return result.Ok();
    }

    [HttpPut("card-limit")]
    public async Task<IActionResult> UpdateCardLimit(
        [FromServices] IFinancialProfileService service,
        [FromBody] UpdateCreditCardLimitModel model)
    {
        var result = await service.UpdateCardLimitAsync(model);
        return result.Ok();
    }

    [HttpPut("caixinha")]
    public async Task<IActionResult> UpdateCaixinhaBalance(
        [FromServices] IFinancialProfileService service,
        [FromBody] decimal balance)
    {
        var result = await service.UpdateCaixinhaBalanceAsync(balance);
        return result.Ok();
    }

    [HttpPost("caixinha/deposit")]
    public async Task<IActionResult> DepositCaixinha(
        [FromServices] IFinancialProfileService service,
        [FromBody] CaixinhaDepositModel model)
    {
        var result = await service.DepositCaixinhaAsync(model.Amount);
        return result.Ok();
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromServices] IFixedCommitmentService commitmentService)
    {
        var allResult = await commitmentService.GetAllAsync(onlyActive: true);
        if (!allResult.IsSuccess || allResult.Value == null)
            return allResult.Ok();

        var subs = allResult.Value
            .Where(c => c.CommitmentType == "SUBSCRIPTION")
            .OrderBy(c => c.BillingDay ?? 1);

        return Ok(subs);
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> AddSubscription(
        [FromServices] IFixedCommitmentService commitmentService,
        [FromBody] CreateFixedCommitmentModel model)
    {
        model.CommitmentType = "SUBSCRIPTION";
        model.TotalInstallments = null;
        var result = await commitmentService.CreateAsync(model);
        return result.Created();
    }

    [HttpDelete("subscriptions/{id:int}")]
    public async Task<IActionResult> DeleteSubscription(
        [FromServices] IFixedCommitmentService commitmentService,
        int id)
    {
        var result = await commitmentService.DeleteAsync(id);
        return result.NoContent();
    }
}
