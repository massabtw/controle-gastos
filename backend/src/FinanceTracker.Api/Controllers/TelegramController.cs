using System.Text.Json;
using FinanceTracker.Api.Services.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramBotService _telegramBotService;

    public TelegramController(ITelegramBotService telegramBotService)
    {
        _telegramBotService = telegramBotService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement update)
    {
        await _telegramBotService.HandleUpdateAsync(update);
        return Ok();
    }
}
