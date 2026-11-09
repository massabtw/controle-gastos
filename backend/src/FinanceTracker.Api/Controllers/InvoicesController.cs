using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Invoices;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    [HttpGet("payment")]
    public async Task<IActionResult> GetPayment(
        [FromServices] IInvoiceService service,
        [FromQuery] string? cycle = null)
    {
        var result = await service.GetInvoicePaymentAsync(cycle);
        return result.Ok();
    }

    [HttpPost("{cycle}/toggle-paid")]
    [HttpPost("{cycle}/unpay")]
    [HttpPost("{cycle}/revert-invoice")]
    [HttpPost("{cycle}/revert")]
    public async Task<IActionResult> TogglePaidByRoute(
        [FromServices] IInvoiceService service,
        string cycle)
    {
        var result = await service.TogglePaymentAsync(cycle);
        return result.Ok();
    }

    [HttpPost("toggle-paid")]
    [HttpPost("unpay")]
    [HttpPost("revert-invoice")]
    public async Task<IActionResult> TogglePaid(
        [FromServices] IInvoiceService service,
        [FromBody] MarkInvoicePaidModel model)
    {
        var result = await service.TogglePaymentAsync(model.Cycle);
        return result.Ok();
    }

    [HttpPost("{cycle}/invest-surplus")]
    public async Task<IActionResult> InvestSurplusByRoute(
        [FromServices] IInvoiceService service,
        string cycle)
    {
        var result = await service.InvestSurplusAsync(cycle);
        return result.Ok();
    }

    [HttpPost("invest-surplus")]
    public async Task<IActionResult> InvestSurplus(
        [FromServices] IInvoiceService service,
        [FromBody] InvestSurplusModel model)
    {
        var result = await service.InvestSurplusAsync(model.Cycle);
        return result.Ok();
    }
}
