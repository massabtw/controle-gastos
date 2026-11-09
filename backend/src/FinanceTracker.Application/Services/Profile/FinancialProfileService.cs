using FinanceTracker.Application.Interfaces.Profile;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.UnitOfWorks;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Profile;

public class FinancialProfileService : IFinancialProfileService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly IValidator<UpdateFinancialProfileModel> _validator;
    private readonly ILogger<FinancialProfileService> _logger;

    public FinancialProfileService(
        IUnitOfWorkApp unitOfWork,
        IValidator<UpdateFinancialProfileModel> validator,
        ILogger<FinancialProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<FinancialProfileModel>> GetProfileAsync()
    {
        try
        {
            var entity = await _unitOfWork.FinancialProfile.GetProfileAsync();
            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial profile");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<FinancialProfileModel>> UpdateProfileAsync(UpdateFinancialProfileModel model)
    {
        var validation = await _validator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<FinancialProfileModel>.Failure(validation.Errors[0].ErrorMessage, 400);
        }

        try
        {
            var existing = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var updated = model.ToEntity(existing);
            var success = await _unitOfWork.FinancialProfile.UpdateProfileAsync(updated);
            if (!success)
            {
                return Result<FinancialProfileModel>.Failure("Falha ao atualizar o perfil financeiro.");
            }

            return updated.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating financial profile");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<bool>> UpdateCaixinhaBalanceAsync(decimal balance)
    {
        if (balance < 0)
        {
            return Result<bool>.Failure("O saldo da caixinha não pode ser negativo.", 400);
        }

        try
        {
            var success = await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(balance);
            return success ? Result<bool>.Success(true) : Result<bool>.Failure("Falha ao atualizar saldo da caixinha.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating caixinha balance to {Balance}", balance);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<decimal>> DepositCaixinhaAsync(decimal amount)
    {
        if (amount <= 0)
        {
            return Result<decimal>.Failure("O valor a adicionar deve ser maior que zero.", 400);
        }

        try
        {
            var newBalance = await _unitOfWork.FinancialProfile.DepositCaixinhaAsync(amount);
            return Result<decimal>.Success(newBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error depositing {Amount} to caixinha", amount);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<bool>> UpdateCardLimitAsync(UpdateCreditCardLimitModel model)
    {
        if (model.CreditCardLimit < 0)
        {
            return Result<bool>.Failure("O limite do cartão não pode ser negativo.", 400);
        }

        try
        {
            var success = await _unitOfWork.FinancialProfile.UpdateCardLimitAsync(model.CreditCardLimit, model.CustomCardCap);
            return success ? Result<bool>.Success(true) : Result<bool>.Failure("Falha ao atualizar limites do cartão.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credit card limit");
            return ErrorMessages.UnknownError;
        }
    }
}
