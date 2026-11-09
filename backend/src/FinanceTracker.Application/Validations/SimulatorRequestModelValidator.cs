using FluentValidation;
using FinanceTracker.Application.Models;

namespace FinanceTracker.Application.Validations;

public class SimulatorRequestModelValidator : AbstractValidator<SimulatorRequestModel>
{
    public SimulatorRequestModelValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor a simular deve ser maior que zero.");

        RuleFor(x => x.Installments)
            .GreaterThanOrEqualTo(1).WithMessage("A quantidade de parcelas deve ser pelo menos 1.");
    }
}
