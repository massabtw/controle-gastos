using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Commitments;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommitmentsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IFixedCommitmentService service,
        [FromQuery] bool onlyActive = true)
    {
        var result = await service.GetAllAsync(onlyActive);
        return result.Ok();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromServices] IFixedCommitmentService service,
        int id)
    {
        var result = await service.GetByIdAsync(id);
        return result.Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromServices] IFixedCommitmentService service,
        [FromBody] CreateFixedCommitmentModel model)
    {
        var result = await service.CreateAsync(model);
        return result.Created();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromServices] IFixedCommitmentService service,
        int id,
        [FromBody] CreateFixedCommitmentModel model)
    {
        var result = await service.UpdateAsync(id, model);
        return result.NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromServices] IFixedCommitmentService service,
        int id)
    {
        var result = await service.DeleteAsync(id);
        return result.NoContent();
    }

    [HttpPost("{id:int}/toggle-paid")]
    public async Task<IActionResult> TogglePaid(
        [FromServices] IFixedCommitmentService service,
        int id,
        [FromQuery] string? cycle = null)
    {
        var result = await service.TogglePaidAsync(id, cycle);
        return result.Ok();
    }
}
