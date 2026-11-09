using FinanceTracker.Application.Interfaces.Commands;
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
        [FromServices] ICommandProcessorService commandProcessor,
        [FromBody] TelegramUpdateModel update)
    {
        if (update.Message == null || string.IsNullOrWhiteSpace(update.Message.Text))
        {
            return Ok();
        }

        var text = update.Message.Text.Trim();
        var chatId = update.Message.Chat.Id;

        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            var welcome = "👋 Olá! Sou o seu assistente financeiro do BurnTracker.\n" +
                          "Aqui você controla seus gastos do cartão e saídas da caixinha em tempo real!\n\n" +
                          "Comandos e exemplos que você pode mandar:\n" +
                          "• Gasto Cartão: 'uber 25', 'almoço 34,90', 'teclado 300 3x'\n" +
                          "• Pix Caixinha: 'pix 40 corte', 'paguei 25 no pix lanche'\n" +
                          "• Receber Pix: 'recebi 50 pix do joao'\n" +
                          "• Adicionar Caixinha: '+10 caixinha'\n" +
                          "• Ajustar Fatura: 'fatura atual 350'\n" +
                          "• Liquidar Fatura: 'paguei a fatura' ou '/pagarfatura'\n" +
                          "• Reinvestir Sobra: 'investir sobra'\n" +
                          "• Atualizar Caixinha: 'saldo caixinha 4500'\n" +
                          "• Simulação: '/simular 300 3x'\n" +
                          "• Resumo do Ciclo: '/status'\n" +
                          "• Desfazer Último: '/desfazer'";

            await telegramService.SendMessageAsync(chatId, welcome);
            return Ok();
        }

        var result = await commandProcessor.ProcessCommandAsync(text);
        if (result.IsSuccess && result.Value != null)
        {
            await telegramService.SendMessageAsync(chatId, result.Value.Reply);
        }

        return Ok();
    }
}
