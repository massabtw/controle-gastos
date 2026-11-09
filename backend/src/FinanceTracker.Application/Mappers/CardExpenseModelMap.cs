using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Mappers;

public static class CardExpenseModelMap
{
    public static ExpenseItemModel ToModel(this CardExpenseEntity entity)
    {
        return new ExpenseItemModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            Description = entity.Description,
            Category = entity.Category,
            Amount = entity.Amount,
            TransactionDate = entity.TransactionDate,
            BillingCycle = entity.BillingCycle,
            InstallmentCurrent = entity.InstallmentCurrent,
            InstallmentTotal = entity.InstallmentTotal,
            PurchaseGroupId = entity.PurchaseGroupId,
            PaymentMethod = entity.PaymentMethod,
            DeductFromCycleCap = entity.DeductFromCycleCap
        };
    }

    public static IEnumerable<ExpenseItemModel> ToModel(this IEnumerable<CardExpenseEntity> entities)
    {
        return entities.Select(ToModel);
    }

    public static CardExpenseEntity ToEntity(this CreateExpenseModel model, string billingCycle, int current = 1, int total = 1, Guid? groupId = null)
    {
        return new CardExpenseEntity
        {
            Description = model.Description,
            Category = string.IsNullOrWhiteSpace(model.Category) ? "Outros" : model.Category,
            Amount = model.Amount,
            TransactionDate = model.TransactionDate ?? DateTime.Now,
            BillingCycle = billingCycle,
            InstallmentCurrent = current,
            InstallmentTotal = total,
            PurchaseGroupId = groupId,
            PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "CARD" : model.PaymentMethod,
            DeductFromCycleCap = model.DeductFromCycleCap ?? true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }
}
