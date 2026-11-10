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

    public async Task<Result<IEnumerable<ExpenseItemModel>>> GetExpensesByCycleAsync(string? cycle = null)
    {
        try
        {
            var targetCycle = cycle;
            if (string.IsNullOrWhiteSpace(targetCycle))
            {
                var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
                targetCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
            }

            var entities = await _unitOfWork.CardExpense.GetByCycleAsync(targetCycle);
            return Result<IEnumerable<ExpenseItemModel>>.Success(entities.ToModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses for cycle {Cycle}", cycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<ExpenseItemModel>> GetExpenseByIdAsync(int id)
    {
        try
        {
            var entity = await _unitOfWork.CardExpense.GetByIdAsync(id);
            if (entity == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<IEnumerable<ExpenseItemModel>>> CreateExpenseAsync(CreateExpenseModel model)
    {
        var validation = await _createValidator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return Result<IEnumerable<ExpenseItemModel>>.Failure(validation.Errors[0].ErrorMessage, 400);
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
                return Result<IEnumerable<ExpenseItemModel>>.Success(new List<ExpenseItemModel> { entity.ToModel() });
            }

            var installments = Math.Max(1, model.Installments);
            if (installments <= 1)
            {
                var entity = model.ToEntity(targetCycle, 1, 1);
                var id = await _unitOfWork.CardExpense.CreateAsync(entity);
                entity.Id = id;
                return Result<IEnumerable<ExpenseItemModel>>.Success(new List<ExpenseItemModel> { entity.ToModel() });
            }

            var groupId = Guid.NewGuid();
            var baseAmount = Math.Floor((model.Amount / installments) * 100m) / 100m;
            var remainder = model.Amount - (baseAmount * installments);

            var batch = new List<CardExpenseEntity>();
            for (var i = 1; i <= installments; i++)
            {
                var currentCycle = BillingCycleCalculator.AddMonthsToCycle(targetCycle, i - 1);
                var val = (i == 1) ? baseAmount + remainder : baseAmount;

                var entity = model.ToEntity(currentCycle, i, installments, groupId);
                entity.Amount = val;
                batch.Add(entity);
            }

            await _unitOfWork.CardExpense.CreateBatchAsync(batch);
            return Result<IEnumerable<ExpenseItemModel>>.Success(batch.ToModel().ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense {Description}", model.Description);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<ExpenseItemModel>> CreatePixCaixinhaExpenseAsync(CreateExpenseModel model)
    {
        model.PaymentMethod = "PIX_CAIXINHA";
        model.Installments = 1;
        var createdResult = await CreateExpenseAsync(model);
        if (!createdResult.IsSuccess || createdResult.Value == null)
        {
            return Result<ExpenseItemModel>.Failure(createdResult.Error ?? ErrorMessages.UnknownError.Message, createdResult.StatusCode);
        }

        return createdResult.Value.First();
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

            await _unitOfWork.FinancialProfile.DepositCaixinhaAsync(model.Amount);

            var entity = model.ToEntity(targetCycle);
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

    public async Task<Result<MessageResponseModel>> UpdateExpenseAsync(int id, UpdateExpenseModel model)
    {
        if (model.Amount <= 0)
        {
            return Result<MessageResponseModel>.Failure("O valor deve ser maior que zero.", 400);
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            return Result<MessageResponseModel>.Failure("A descrição é obrigatória.", 400);
        }

        try
        {
            var existing = await _unitOfWork.CardExpense.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = BillingCycleCalculator.GetTargetBillingCycle(model.TransactionDate, profile.ClosingDay);

            if (model.UpdateAllGroupInstallments && existing.PurchaseGroupId.HasValue)
            {
                await _unitOfWork.CardExpense.UpdateGroupDetailsAsync(
                    existing.PurchaseGroupId.Value,
                    model.Description.Trim(),
                    string.IsNullOrWhiteSpace(model.Category) ? "Outros" : model.Category.Trim());
            }

            existing.ApplyUpdate(model, targetCycle);
            var success = await _unitOfWork.CardExpense.UpdateAsync(existing);
            if (!success)
            {
                return ErrorMessages.EntityNotFound;
            }

            return new MessageResponseModel { Message = "Despesa atualizada com sucesso." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<MessageResponseModel>> DeleteExpenseAsync(int id, bool deleteAllInstallments = false)
    {
        try
        {
            var expense = await _unitOfWork.CardExpense.GetByIdAsync(id);
            if (expense == null)
            {
                return ErrorMessages.EntityNotFound;
            }

            await AdjustCaixinhaOnDeleteAsync(expense);

            if (deleteAllInstallments && expense.PurchaseGroupId.HasValue)
            {
                var count = await _unitOfWork.CardExpense.DeleteByPurchaseGroupIdAsync(expense.PurchaseGroupId.Value);
                return count > 0
                    ? new MessageResponseModel { Message = "Todas as parcelas foram removidas com sucesso." }
                    : ErrorMessages.EntityNotFound;
            }

            var success = await _unitOfWork.CardExpense.DeleteAsync(id);
            return success
                ? new MessageResponseModel { Message = "Despesa removida com sucesso." }
                : ErrorMessages.EntityNotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {Id}", id);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<MessageResponseModel>> DeleteLastExpenseAsync()
    {
        try
        {
            var last = await _unitOfWork.CardExpense.GetLastExpenseAsync();
            if (last == null)
            {
                return Result<MessageResponseModel>.Failure("Nenhum gasto recente encontrado para desfazer.", 404);
            }

            await AdjustCaixinhaOnDeleteAsync(last);

            if (last.PurchaseGroupId.HasValue)
            {
                var group = (await _unitOfWork.CardExpense.GetByGroupIdAsync(last.PurchaseGroupId.Value)).ToList();
                var total = group.Sum(x => x.Amount);
                var desc = group.FirstOrDefault()?.Description ?? last.Description;
                var installments = group.Count;

                await _unitOfWork.CardExpense.DeleteByPurchaseGroupIdAsync(last.PurchaseGroupId.Value);
                return new MessageResponseModel
                {
                    Message = $"Desfeito com sucesso: Compra '{desc}' (R$ {total:F2} em {installments}x)."
                };
            }

            await _unitOfWork.CardExpense.DeleteAsync(last.Id);
            return new MessageResponseModel
            {
                Message = $"Desfeito com sucesso: Gasto '{last.Description}' (R$ {last.Amount:F2})."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting last expense");
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<SyncInvoiceResultModel>> SyncInvoiceAsync(SyncInvoiceModel model)
    {
        if (model.TargetTotal < 0)
        {
            return Result<SyncInvoiceResultModel>.Failure("O valor da fatura não pode ser negativo.", 400);
        }

        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = string.IsNullOrWhiteSpace(model.Cycle)
                ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
                : model.Cycle;

            var expenses = (await _unitOfWork.CardExpense.GetByCycleAsync(targetCycle)).ToList();
            var allCommitments = (await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true)).ToList();
            var cardSubscriptionsTotal = allCommitments
                .Where(c => string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(c.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrWhiteSpace(c.StartCycle) || string.CompareOrdinal(targetCycle, c.StartCycle) >= 0))
                .Sum(c => c.Amount);

            var currentTotal = expenses.Where(e => e.PaymentMethod == "CARD").Sum(e => e.Amount) + cardSubscriptionsTotal;
            var diff = model.TargetTotal - currentTotal;

            if (Math.Abs(diff) >= 0.01m)
            {
                var adjustment = CardExpenseModelMap.ToInvoiceSyncEntity(diff, targetCycle);
                await _unitOfWork.CardExpense.CreateAsync(adjustment);
            }

            return new SyncInvoiceResultModel
            {
                Message = "Fatura sincronizada com sucesso.",
                CurrentInvoiceTotal = model.TargetTotal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoice total");
            return ErrorMessages.UnknownError;
        }
    }

    private async Task AdjustCaixinhaOnDeleteAsync(CardExpenseEntity expense)
    {
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
    }
}
