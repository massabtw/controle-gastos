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

    public async Task<Result<ProfileWithCommitmentsModel>> GetProfileWithCommitmentsAsync()
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var commitments = await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: false);

            return new ProfileWithCommitmentsModel
            {
                Profile = profile.ToModel(),
                Commitments = commitments.ToModel().ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial profile with commitments");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<UpdateProfileResultModel>> UpdateProfileAsync(UpdateFinancialProfileModel model)
    {
        var validation = await _validator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<UpdateProfileResultModel>.Failure(validation.Errors[0].ErrorMessage, 400);
        }

        try
        {
            var existing = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var updated = model.ToEntity(existing);
            var success = await _unitOfWork.FinancialProfile.UpdateProfileAsync(updated);
            if (!success)
            {
                return Result<UpdateProfileResultModel>.Failure("Falha ao atualizar o perfil financeiro.");
            }

            return new UpdateProfileResultModel
            {
                Message = "Perfil atualizado com sucesso.",
                Profile = updated.ToModel()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating financial profile");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<CaixinhaOperationResultModel>> UpdateCaixinhaBalanceAsync(decimal balance)
    {
        if (balance < 0)
        {
            return Result<CaixinhaOperationResultModel>.Failure("O saldo da caixinha não pode ser negativo.", 400);
        }

        try
        {
            var success = await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(balance);
            return success
                ? new CaixinhaOperationResultModel
                {
                    Message = "Saldo da Caixinha atualizado com sucesso.",
                    Balance = balance,
                    Amount = balance
                }
                : Result<CaixinhaOperationResultModel>.Failure("Falha ao atualizar saldo da caixinha.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating caixinha balance to {Balance}", balance);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<CaixinhaOperationResultModel>> DepositCaixinhaAsync(decimal amount)
    {
        if (amount <= 0)
        {
            return Result<CaixinhaOperationResultModel>.Failure("O valor a adicionar deve ser maior que zero.", 400);
        }

        try
        {
            var newBalance = await _unitOfWork.FinancialProfile.DepositCaixinhaAsync(amount);
            return new CaixinhaOperationResultModel
            {
                Message = $"R$ {amount:F2} adicionados à Caixinha com sucesso.",
                Balance = newBalance,
                Amount = amount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error depositing {Amount} to caixinha", amount);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<UpdateCardLimitResultModel>> UpdateCardLimitAsync(UpdateCreditCardLimitModel model)
    {
        if (model.CreditCardLimit <= 0)
        {
            return Result<UpdateCardLimitResultModel>.Failure("O limite do cartão deve ser maior que zero.", 400);
        }

        if (model.CustomCardCap.HasValue && model.CustomCardCap.Value <= 0)
        {
            model.CustomCardCap = null;
        }

        try
        {
            var success = await _unitOfWork.FinancialProfile.UpdateCardLimitAsync(model.CreditCardLimit, model.CustomCardCap);
            return success
                ? new UpdateCardLimitResultModel
                {
                    Message = "Limites do cartão atualizados com sucesso.",
                    CreditCardLimit = model.CreditCardLimit,
                    CustomCardCap = model.CustomCardCap
                }
                : Result<UpdateCardLimitResultModel>.Failure("Falha ao atualizar limites do cartão.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credit card limit");
            return ErrorMessages.UnknownError;
        }
    }
}
