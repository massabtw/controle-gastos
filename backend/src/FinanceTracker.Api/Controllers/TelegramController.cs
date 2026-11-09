using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Http.Telegram.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook(
        [FromServices] ITelegramService telegramService,
        [FromBody] TelegramUpdateModel update)
    {
        await telegramService.SendMessageAsync(update.Message?.Chat.Id ?? 0, "Webhook recebido");
        return Ok();
    }
}
