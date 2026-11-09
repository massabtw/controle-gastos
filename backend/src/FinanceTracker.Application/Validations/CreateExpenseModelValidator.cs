using FluentValidation;
using FinanceTracker.Application.Models;

namespace FinanceTracker.Application.Validations;

public class CreateExpenseModelValidator : AbstractValidator<CreateExpenseModel>
{
    public CreateExpenseModelValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(200).WithMessage("A descrição não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor do gasto deve ser maior que zero.");

        RuleFor(x => x.Installments)
            .GreaterThanOrEqualTo(1).WithMessage("A quantidade de parcelas deve ser pelo menos 1.");
    }
}
