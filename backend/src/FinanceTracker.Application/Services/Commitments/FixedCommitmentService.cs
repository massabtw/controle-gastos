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

    public async Task<Result<bool>> UpdateAsync(int id, CreateFixedCommitmentModel model)
    {
        var validation = await _validator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<bool>.Failure(validation.Errors[0].ErrorMessage, 400);
        }

        try
        {
            var existing = await _unitOfWork.FixedCommitment.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            existing.Title = model.Title;
            existing.Amount = model.Amount;
            if (!string.IsNullOrWhiteSpace(model.StartCycle))
                existing.StartCycle = model.StartCycle;
            existing.TotalInstallments = model.TotalInstallments;
            existing.CommitmentType = model.CommitmentType ?? existing.CommitmentType;
            existing.PaymentSource = model.PaymentSource ?? existing.PaymentSource;
            existing.BillingDay = model.BillingDay ?? existing.BillingDay;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            var success = await _unitOfWork.FixedCommitment.UpdateAsync(existing);
            return success ? Result<bool>.Success(true) : Result<bool>.Failure("Falha ao atualizar compromisso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fixed commitment {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var success = await _unitOfWork.FixedCommitment.DeleteAsync(id);
            return success ? Result<bool>.Success(true) : ErrorMessages.EntityNotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fixed commitment {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<object>> TogglePaidAsync(int id, string? cycle)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = cycle ?? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);

            var (success, isPaid, balanceAfter) = await _unitOfWork.FixedCommitment.ToggleCommitmentPaidAsync(id, targetCycle);
            if (!success)
            {
                return Result<object>.Failure("Compromisso não encontrado ou inativo.", 404);
            }

            return Result<object>.Success(new
            {
                success = true,
                isPaid,
                targetCycle,
                newCaixinhaBalance = balanceAfter,
                message = isPaid
                    ? $"Compromisso marcado como pago no ciclo {targetCycle}. Saldo deduzido da Caixinha."
                    : $"Compromisso desmarcado como pago. Saldo estornado para a Caixinha."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling commitment {Id} paid state for cycle {Cycle}", id, cycle);
            return ErrorMessages.UnknownError;
        }
    }
}
