using FinanceTracker.Application.Interfaces.Commitments;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.Services;
using FinanceTracker.Domain.UnitOfWorks;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Commitments;

public class FixedCommitmentService : IFixedCommitmentService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly IValidator<CreateFixedCommitmentModel> _validator;
    private readonly ILogger<FixedCommitmentService> _logger;

    public FixedCommitmentService(
        IUnitOfWorkApp unitOfWork,
        IValidator<CreateFixedCommitmentModel> validator,
        ILogger<FixedCommitmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<FixedCommitmentModel>>> GetAllAsync(bool onlyActive = true)
    {
        try
        {
            var entities = await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive);
            return Result<IEnumerable<FixedCommitmentModel>>.Success(entities.ToModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fixed commitments");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<IEnumerable<FixedCommitmentModel>>> GetSubscriptionsAsync()
    {
        try
        {
            var entities = await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true);
            var subs = entities
                .Where(c => string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.BillingDay ?? 1)
                .ToModel();
            return Result<IEnumerable<FixedCommitmentModel>>.Success(subs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<FixedCommitmentModel>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await _unitOfWork.FixedCommitment.GetByIdAsync(id);
            if (entity == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fixed commitment by id {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<FixedCommitmentModel>> CreateAsync(CreateFixedCommitmentModel model)
    {
        var validation = await _validator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<FixedCommitmentModel>.Failure(validation.Errors[0].ErrorMessage, 400);
        }

        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var currentCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);

            var entity = model.ToEntity(currentCycle);
            var id = await _unitOfWork.FixedCommitment.CreateAsync(entity);
            entity.Id = id;
            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fixed commitment {Title}", model.Title);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<FixedCommitmentModel>> CreateSubscriptionAsync(CreateFixedCommitmentModel model)
    {
        model.CommitmentType = "SUBSCRIPTION";
        model.TotalInstallments = null;
        return await CreateAsync(model);
    }

    public async Task<Result<MessageResponseModel>> UpdateAsync(int id, CreateFixedCommitmentModel model)
    {
        try
        {
            var existing = await _unitOfWork.FixedCommitment.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            existing.ApplyUpdate(model);

            var success = await _unitOfWork.FixedCommitment.UpdateAsync(existing);
            return success
                ? new MessageResponseModel { Message = "Compromisso atualizado com sucesso." }
                : Result<MessageResponseModel>.Failure("Falha ao atualizar compromisso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fixed commitment {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<MessageResponseModel>> DeleteAsync(int id)
    {
        try
        {
            var success = await _unitOfWork.FixedCommitment.DeleteAsync(id);
            return success
                ? new MessageResponseModel { Message = "Compromisso removido com sucesso." }
                : ErrorMessages.EntityNotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fixed commitment {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<TogglePaidResultModel>> TogglePaidAsync(int id, string? cycle)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = string.IsNullOrWhiteSpace(cycle)
                ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
                : cycle;

            var (success, isPaid, balanceAfter) = await _unitOfWork.FixedCommitment.ToggleCommitmentPaidAsync(id, targetCycle);
            if (!success)
            {
                return Result<TogglePaidResultModel>.Failure("Compromisso não encontrado ou inativo.", 404);
            }

            return new TogglePaidResultModel
            {
                IsPaid = isPaid,
                Cycle = targetCycle,
                TargetCycle = targetCycle,
                BalanceAfter = balanceAfter,
                NewCaixinhaBalance = balanceAfter,
                Message = isPaid
                    ? $"Compromisso marcado como pago no ciclo {targetCycle}. Saldo deduzido da Caixinha."
                    : $"Compromisso desmarcado como pago. Saldo estornado para a Caixinha."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling commitment {Id} paid state for cycle {Cycle}", id, cycle);
            return ErrorMessages.UnknownError;
        }
    }
}
