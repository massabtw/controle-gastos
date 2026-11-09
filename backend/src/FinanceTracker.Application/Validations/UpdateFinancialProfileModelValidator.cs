using FluentValidation;
using FinanceTracker.Application.Models;

namespace FinanceTracker.Application.Validations;

public class UpdateFinancialProfileModelValidator : AbstractValidator<UpdateFinancialProfileModel>
{
    public UpdateFinancialProfileModelValidator()
    {
        RuleFor(x => x.IncomeFirstTranche)
            .GreaterThanOrEqualTo(0).WithMessage("O primeiro adiantamento não pode ser negativo.");

        RuleFor(x => x.IncomeSecondTranche)
            .GreaterThanOrEqualTo(0).WithMessage("O segundo pagamento não pode ser negativo.");

        RuleFor(x => x.MonthlyInvestmentGoal)
            .GreaterThanOrEqualTo(0).WithMessage("A meta mensal de investimento não pode ser negativa.");

        RuleFor(x => x.ClosingDay)
            .InclusiveBetween(1, 31).WithMessage("O dia de fechamento deve ser entre 1 e 31.");

        RuleFor(x => x.DueDay)
            .InclusiveBetween(1, 31).WithMessage("O dia de vencimento deve ser entre 1 e 31.");

        RuleFor(x => x.NubankCaixinhaBalance)
            .GreaterThanOrEqualTo(0).WithMessage("O saldo da caixinha não pode ser negativo.");

        RuleFor(x => x.CreditCardLimit)
            .GreaterThanOrEqualTo(0).WithMessage("O limite do cartão não pode ser negativo.");
    }
}
