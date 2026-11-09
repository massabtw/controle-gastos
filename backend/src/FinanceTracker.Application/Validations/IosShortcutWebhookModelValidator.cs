using FluentValidation;
using FinanceTracker.Application.Models;

namespace FinanceTracker.Application.Validations;

public class IosShortcutWebhookModelValidator : AbstractValidator<IosShortcutWebhookModel>
{
    public IosShortcutWebhookModelValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Merchant) || !string.IsNullOrWhiteSpace(x.RawText))
            .WithMessage("Ao menos um campo entre 'Merchant' ou 'RawText' deve ser informado.");

        When(x => x.Installments.HasValue, () =>
        {
            RuleFor(x => x.Installments!.Value)
                .GreaterThan(0)
                .WithMessage("A quantidade de parcelas deve ser maior que zero.");
        });
    }
}
