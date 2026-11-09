using FluentValidation;
using FinanceTracker.Application.Models;

namespace FinanceTracker.Application.Validations;

public class CreateFixedCommitmentModelValidator : AbstractValidator<CreateFixedCommitmentModel>
{
    public CreateFixedCommitmentModelValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título do compromisso é obrigatório.")
            .MaximumLength(150).WithMessage("O título não pode ter mais de 150 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");
    }
}
