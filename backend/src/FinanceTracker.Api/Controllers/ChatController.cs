using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Commands;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessMessage(
        [FromServices] ICommandProcessorService service,
        [FromBody] ChatMessageModel model)
    {
        var result = await service.ProcessCommandAsync(model.Message);
        return result.Ok();
    }
}
