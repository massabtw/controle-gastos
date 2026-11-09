using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromServices] IExpenseService service,
        [FromQuery] string cycle)
    {
        if (string.IsNullOrWhiteSpace(cycle))
            return BadRequest(new { message = "Parameter 'cycle' is required (format: YYYY-MM)." });

        var result = await service.GetExpensesByCycleAsync(cycle);
        return result.Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense(
        [FromServices] IExpenseService service,
        [FromBody] CreateExpenseModel model)
    {
        var result = await service.CreateExpenseAsync(model);
        return result.Created();
    }

    [HttpPost("receive-pix")]
    public async Task<IActionResult> ReceivePix(
        [FromServices] IExpenseService service,
        [FromBody] ReceivePixModel model)
    {
        var result = await service.ReceivePixAsync(model);
        return result.Created();
    }

    [HttpPost("pix-caixinha")]
    public async Task<IActionResult> CreatePixCaixinhaExpense(
        [FromServices] IExpenseService service,
        [FromBody] CreateExpenseModel model)
    {
        model.PaymentMethod = "PIX_CAIXINHA";
        model.Installments = 1;
        var result = await service.CreateExpenseAsync(model);
        return result.Created();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(
        [FromServices] IExpenseService service,
        int id)
    {
        var result = await service.DeleteExpenseAsync(id);
        return result.NoContent();
    }

    [HttpPost("undo-last")]
    public async Task<IActionResult> UndoLastExpense(
        [FromServices] IExpenseService service)
    {
        var result = await service.DeleteLastExpenseAsync();
        return result.Ok();
    }
}
