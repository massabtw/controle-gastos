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
            Description = model.Description.Trim(),
            Category = string.IsNullOrWhiteSpace(model.Category) ? "Outros" : model.Category.Trim(),
            Amount = model.Amount,
            TransactionDate = model.TransactionDate ?? DateTime.Now,
            BillingCycle = billingCycle,
            InstallmentCurrent = current,
            InstallmentTotal = total,
            PurchaseGroupId = groupId,
            PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "CARD" : model.PaymentMethod.ToUpperInvariant(),
            DeductFromCycleCap = model.DeductFromCycleCap ?? true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }

    public static CardExpenseEntity ToEntity(this ReceivePixModel model, string billingCycle)
    {
        return new CardExpenseEntity
        {
            Description = string.IsNullOrWhiteSpace(model.Description) ? "Pix Recebido" : model.Description.Trim(),
            Category = string.IsNullOrWhiteSpace(model.Category) ? "Entrada" : model.Category.Trim(),
            Amount = model.Amount,
            TransactionDate = model.TransactionDate ?? DateTime.Now,
            BillingCycle = billingCycle,
            InstallmentCurrent = 1,
            InstallmentTotal = 1,
            PurchaseGroupId = null,
            PaymentMethod = "PIX_INFLOW",
            DeductFromCycleCap = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }

    public static CardExpenseEntity ToInvoiceSyncEntity(decimal diff, string billingCycle)
    {
        return new CardExpenseEntity
        {
            Description = diff > 0 ? "Ajuste de Fatura (Nubank)" : "Ajuste de Fatura (Crédito/Estorno)",
            Category = "Outros",
            Amount = diff,
            TransactionDate = DateTime.Now,
            BillingCycle = billingCycle,
            InstallmentCurrent = 1,
            InstallmentTotal = 1,
            PurchaseGroupId = null,
            PaymentMethod = "CARD",
            DeductFromCycleCap = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }

    public static void ApplyUpdate(this CardExpenseEntity existing, UpdateExpenseModel model, string targetCycle)
    {
        existing.Description = model.Description.Trim();
        existing.Category = string.IsNullOrWhiteSpace(model.Category) ? "Outros" : model.Category.Trim();
        existing.Amount = model.Amount;
        existing.TransactionDate = model.TransactionDate;
        existing.BillingCycle = targetCycle;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
