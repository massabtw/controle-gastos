using FinanceTracker.Api.Extensions;
using FinanceTracker.Application.Interfaces.Integrations;
using FinanceTracker.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntegrationsController : ControllerBase
{
    [HttpPost("ios-shortcut")]
    public async Task<IActionResult> HandleIosShortcut(
        [FromServices] IIosIntegrationService integrationService,
        [FromBody] IosShortcutWebhookModel model)
    {
        var result = await integrationService.ProcessShortcutWebhookAsync(model);
        return result.Ok();
    }
}
