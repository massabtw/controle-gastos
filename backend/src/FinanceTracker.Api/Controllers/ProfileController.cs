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
        var result = await service.GetProfileWithCommitmentsAsync();
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

    [HttpPatch("card-limit")]
    [HttpPut("card-limit")]
    public async Task<IActionResult> UpdateCardLimit(
        [FromServices] IFinancialProfileService service,
        [FromBody] UpdateCreditCardLimitModel model)
    {
        var result = await service.UpdateCardLimitAsync(model);
        return result.Ok();
    }

    [HttpPost("caixinha")]
    [HttpPut("caixinha")]
    public async Task<IActionResult> UpdateCaixinhaBalance(
        [FromServices] IFinancialProfileService service,
        [FromBody] UpdateCaixinhaBalanceModel model)
    {
        var result = await service.UpdateCaixinhaBalanceAsync(model.Balance);
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

    [HttpPost("commitments")]
    public async Task<IActionResult> AddCommitment(
        [FromServices] IFixedCommitmentService commitmentService,
        [FromBody] CreateFixedCommitmentModel model)
    {
        var result = await commitmentService.CreateAsync(model);
        return result.Created();
    }

    [HttpPut("commitments/{id:int}")]
    public async Task<IActionResult> UpdateCommitment(
        [FromServices] IFixedCommitmentService commitmentService,
        int id,
        [FromBody] CreateFixedCommitmentModel model)
    {
        var result = await commitmentService.UpdateAsync(id, model);
        return result.Ok();
    }

    [HttpDelete("commitments/{id:int}")]
    public async Task<IActionResult> DeleteCommitment(
        [FromServices] IFixedCommitmentService commitmentService,
        int id)
    {
        var result = await commitmentService.DeleteAsync(id);
        return result.Ok();
    }

    [HttpPost("commitments/{id:int}/toggle-paid")]
    [HttpPost("commitments/{id:int}/pay-from-caixinha")]
    public async Task<IActionResult> ToggleCommitmentPaid(
        [FromServices] IFixedCommitmentService commitmentService,
        int id,
        [FromQuery] string? cycle = null)
    {
        var result = await commitmentService.TogglePaidAsync(id, cycle);
        return result.Ok();
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromServices] IFixedCommitmentService commitmentService)
    {
        var result = await commitmentService.GetSubscriptionsAsync();
        return result.Ok();
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> AddSubscription(
        [FromServices] IFixedCommitmentService commitmentService,
        [FromBody] CreateFixedCommitmentModel model)
    {
        var result = await commitmentService.CreateSubscriptionAsync(model);
        return result.Created();
    }

    [HttpDelete("subscriptions/{id:int}")]
    public async Task<IActionResult> DeleteSubscription(
        [FromServices] IFixedCommitmentService commitmentService,
        int id)
    {
        var result = await commitmentService.DeleteAsync(id);
        return result.Ok();
    }
}
