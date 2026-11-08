using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses([FromQuery] string? cycle)
    {
        var expenses = await _expenseService.GetExpensesByCycleAsync(cycle);
        return Ok(expenses);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
            return NotFound(new { message = $"Expense {id} not found" });

        return Ok(expense);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
    {
        if (dto.TotalAmount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Description is required." });

        var created = await _expenseService.CreateExpenseAsync(dto);
        return StatusCode(201, created);
    }

    [HttpPost("pix-caixinha")]
    public async Task<IActionResult> CreatePixCaixinha([FromBody] CreatePixCaixinhaDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Description is required." });

        var created = await _expenseService.CreatePixCaixinhaExpenseAsync(dto);
        return StatusCode(201, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Description is required." });

        var success = await _expenseService.UpdateExpenseAsync(id, dto);
        if (!success)
            return NotFound(new { message = $"Expense {id} not found." });

        return Ok(new { message = "Expense updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(int id, [FromQuery] bool deleteAllInstallments = false)
    {
        var success = await _expenseService.DeleteExpenseAsync(id, deleteAllInstallments);
        if (!success)
            return NotFound(new { message = $"Expense {id} not found." });

        return Ok(new { message = "Expense deleted successfully." });
    }

    [HttpPost("undo")]
    public async Task<IActionResult> UndoLatest()
    {
        var message = await _expenseService.UndoLatestExpenseAsync();
        return Ok(new { message });
    }

    [HttpPost("sync-invoice")]
    public async Task<IActionResult> SyncInvoice([FromBody] SyncInvoiceDto dto)
    {
        if (dto.TargetTotal < 0)
            return BadRequest(new { message = "Target total cannot be negative." });

        var newTotal = await _expenseService.SyncInvoiceTotalAsync(dto.TargetTotal, dto.Cycle);
        return Ok(new { message = "Invoice synchronized successfully.", currentInvoiceTotal = newTotal });
    }
}
