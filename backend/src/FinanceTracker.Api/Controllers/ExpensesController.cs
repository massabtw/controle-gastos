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
        [FromQuery] string? cycle = null)
    {
        var result = await service.GetExpensesByCycleAsync(cycle);
        return result.Ok();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExpenseById(
        [FromServices] IExpenseService service,
        int id)
    {
        var result = await service.GetExpenseByIdAsync(id);
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
        var result = await service.CreatePixCaixinhaExpenseAsync(model);
        return result.Created();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateExpense(
        [FromServices] IExpenseService service,
        int id,
        [FromBody] UpdateExpenseModel model)
    {
        var result = await service.UpdateExpenseAsync(id, model);
        return result.Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(
        [FromServices] IExpenseService service,
        int id,
        [FromQuery] bool deleteAllInstallments = false)
    {
        var result = await service.DeleteExpenseAsync(id, deleteAllInstallments);
        return result.Ok();
    }

    [HttpPost("undo")]
    [HttpPost("undo-last")]
    public async Task<IActionResult> UndoLastExpense(
        [FromServices] IExpenseService service)
    {
        var result = await service.DeleteLastExpenseAsync();
        return result.Ok();
    }

    [HttpPost("sync-invoice")]
    public async Task<IActionResult> SyncInvoice(
        [FromServices] IExpenseService service,
        [FromBody] SyncInvoiceModel model)
    {
        var result = await service.SyncInvoiceAsync(model);
        return result.Ok();
    }
}
