using FinanceTracker.Application.Interfaces.Commands;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Http.Telegram.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook(
        [FromServices] ITelegramService telegramService,
        [FromServices] ICommandProcessorService commandProcessor,
        [FromServices] IOptions<TelegramOptions> options,
        [FromBody] TelegramUpdateModel update)
    {
        if (update.Message == null || string.IsNullOrWhiteSpace(update.Message.Text))
        {
            return Ok();
        }

        var chatId = update.Message.Chat.Id;
        var allowedChatId = options.Value.AllowedChatId;
        if (allowedChatId != 0 && chatId != allowedChatId)
        {
            await telegramService.SendMessageAsync(chatId, "⛔ Acesso não autorizado para este ChatId.");
            return Ok();
        }

        var result = await commandProcessor.ProcessCommandAsync(update.Message.Text.Trim());
        if (result.IsSuccess && result.Value != null)
        {
            await telegramService.SendMessageAsync(chatId, result.Value.Reply);
        }

        return Ok();
    }
}
