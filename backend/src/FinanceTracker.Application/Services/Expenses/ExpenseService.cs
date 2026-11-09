using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.Services;
using FinanceTracker.Domain.UnitOfWorks;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly IValidator<CreateExpenseModel> _createValidator;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(
        IUnitOfWorkApp unitOfWork,
        IValidator<CreateExpenseModel> createValidator,
        ILogger<ExpenseService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ExpenseItemModel>>> GetExpensesByCycleAsync(string cycle)
    {
        try
        {
            var entities = await _unitOfWork.CardExpense.GetByCycleAsync(cycle);
            return Result<IEnumerable<ExpenseItemModel>>.Success(entities.ToModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses for cycle {Cycle}", cycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<ExpenseItemModel>> CreateExpenseAsync(CreateExpenseModel model)
    {
        var validation = await _createValidator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<ExpenseItemModel>.Failure(validation.Errors[0].ErrorMessage, 400);
        }

        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = BillingCycleCalculator.GetTargetBillingCycle(model.TransactionDate ?? DateTime.Now, profile.ClosingDay);
            var isPixCaixinha = model.PaymentMethod?.Equals("PIX_CAIXINHA", StringComparison.OrdinalIgnoreCase) == true;

            if (isPixCaixinha)
            {
                var newBalance = Math.Max(0, profile.NubankCaixinhaBalance - model.Amount);
                await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(newBalance);

                var entity = model.ToEntity(targetCycle, 1, 1);
                entity.PaymentMethod = "PIX_CAIXINHA";
                entity.DeductFromCycleCap = model.DeductFromCycleCap ?? true;

                var id = await _unitOfWork.CardExpense.CreateAsync(entity);
                entity.Id = id;
                return entity.ToModel();
            }

            if (model.Installments <= 1)
            {
                var entity = model.ToEntity(targetCycle, 1, 1);
                var id = await _unitOfWork.CardExpense.CreateAsync(entity);
                entity.Id = id;
                return entity.ToModel();
            }

            // Multiple installments
            var groupId = Guid.NewGuid();
            var installmentValue = Math.Round(model.Amount / model.Installments, 2);
            var remainder = model.Amount - (installmentValue * model.Installments);

            var batch = new List<CardExpenseEntity>();
            for (var i = 1; i <= model.Installments; i++)
            {
                var currentCycle = BillingCycleCalculator.AddMonthsToCycle(targetCycle, i - 1);
                var val = (i == 1) ? installmentValue + remainder : installmentValue;

                var entity = model.ToEntity(currentCycle, i, model.Installments, groupId);
                entity.Amount = val;
                batch.Add(entity);
            }

            await _unitOfWork.CardExpense.CreateBatchAsync(batch);
            return batch[0].ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense {Description}", model.Description);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<ExpenseItemModel>> ReceivePixAsync(ReceivePixModel model)
    {
        if (model.Amount <= 0)
        {
            return Result<ExpenseItemModel>.Failure("O valor recebido deve ser maior que zero.", 400);
        }

        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = BillingCycleCalculator.GetTargetBillingCycle(model.TransactionDate ?? DateTime.Now, profile.ClosingDay);

            var newBalance = profile.NubankCaixinhaBalance + model.Amount;
            await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(newBalance);

            var entity = new CardExpenseEntity
            {
                Description = string.IsNullOrWhiteSpace(model.Description) ? "Pix Recebido" : model.Description,
                Category = string.IsNullOrWhiteSpace(model.Category) ? "Entrada Pix" : model.Category,
                Amount = model.Amount,
                TransactionDate = model.TransactionDate ?? DateTime.Now,
                BillingCycle = targetCycle,
                InstallmentCurrent = 1,
                InstallmentTotal = 1,
                PaymentMethod = "PIX_INFLOW",
                DeductFromCycleCap = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Deleted = false
            };

            var id = await _unitOfWork.CardExpense.CreateAsync(entity);
            entity.Id = id;
            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording received Pix");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<bool>> DeleteExpenseAsync(int id)
    {
        try
        {
            var expense = await _unitOfWork.CardExpense.GetByIdAsync(id);
            if (expense == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            if (expense.PaymentMethod == "PIX_CAIXINHA")
            {
                var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
                await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(profile.NubankCaixinhaBalance + expense.Amount);
            }
            else if (expense.PaymentMethod == "PIX_INFLOW")
            {
                var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
                await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(Math.Max(0, profile.NubankCaixinhaBalance - expense.Amount));
            }

            var success = await _unitOfWork.CardExpense.DeleteAsync(id);
            return success ? Result<bool>.Success(true) : ErrorMessages.EntityNotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<bool>> DeleteLastExpenseAsync()
    {
        try
        {
            var last = await _unitOfWork.CardExpense.GetLastExpenseAsync();
            if (last == null)
            {
                return Result<bool>.Failure("Nenhum gasto recente encontrado para desfazer.", 404);
            }

            if (last.PaymentMethod == "PIX_CAIXINHA")
            {
                var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
                await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(profile.NubankCaixinhaBalance + last.Amount);
            }
            else if (last.PaymentMethod == "PIX_INFLOW")
            {
                var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
                await _unitOfWork.FinancialProfile.UpdateCaixinhaBalanceAsync(Math.Max(0, profile.NubankCaixinhaBalance - last.Amount));
            }

            if (last.PurchaseGroupId.HasValue)
            {
                await _unitOfWork.CardExpense.DeleteByPurchaseGroupIdAsync(last.PurchaseGroupId.Value);
            }
            else
            {
                await _unitOfWork.CardExpense.DeleteAsync(last.Id);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting last expense");
            return ErrorMessages.UnknownError;
        }
    }
}
