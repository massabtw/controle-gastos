using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Integrations;

public interface IIosIntegrationService
{
    Task<Result<IosShortcutResponseModel>> ProcessShortcutWebhookAsync(IosShortcutWebhookModel model);
}
