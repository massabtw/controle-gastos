using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ICommandProcessorService _commandProcessor;

    public ChatController(ICommandProcessorService commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [HttpPost("command")]
    public async Task<IActionResult> ExecuteCommand([FromBody] ChatCommandRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "Message is required." });

        var response = await _commandProcessor.ProcessCommandAsync(dto.Message);
        return Ok(response);
    }
}
