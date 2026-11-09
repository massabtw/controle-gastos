using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Mappers;

public static class FixedCommitmentModelMap
{
    public static FixedCommitmentModel ToModel(this FixedCommitmentEntity entity)
    {
        return new FixedCommitmentModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            Title = entity.Title,
            Amount = entity.Amount,
            StartCycle = entity.StartCycle,
            TotalInstallments = entity.TotalInstallments,
            IsActive = entity.IsActive,
            CommitmentType = entity.CommitmentType,
            PaymentSource = entity.PaymentSource,
            BillingDay = entity.BillingDay,
            LastPaidCycle = entity.LastPaidCycle
        };
    }

    public static IEnumerable<FixedCommitmentModel> ToModel(this IEnumerable<FixedCommitmentEntity> entities)
    {
        return entities.Select(ToModel);
    }

    public static FixedCommitmentEntity ToEntity(this CreateFixedCommitmentModel model, string startCycle)
    {
        var resolvedStartCycle = !string.IsNullOrWhiteSpace(model.StartCycle) ? model.StartCycle : startCycle;
        if (string.Equals(model.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
            string.CompareOrdinal(resolvedStartCycle, startCycle) < 0)
        {
            resolvedStartCycle = startCycle;
        }

        return new FixedCommitmentEntity
        {
            Title = model.Title.Trim(),
            Amount = model.Amount,
            StartCycle = resolvedStartCycle,
            TotalInstallments = model.TotalInstallments,
            IsActive = model.IsActive ?? true,
            CommitmentType = string.IsNullOrWhiteSpace(model.CommitmentType) ? "PIX_COMMITMENT" : model.CommitmentType,
            PaymentSource = string.IsNullOrWhiteSpace(model.PaymentSource) ? "PIX_CAIXINHA" : model.PaymentSource,
            BillingDay = model.BillingDay ?? 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }

    public static void ApplyUpdate(this FixedCommitmentEntity existing, CreateFixedCommitmentModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Title))
            existing.Title = model.Title.Trim();
        if (model.Amount > 0)
            existing.Amount = model.Amount;
        if (!string.IsNullOrWhiteSpace(model.StartCycle))
            existing.StartCycle = model.StartCycle;
        existing.TotalInstallments = model.TotalInstallments;
        if (model.IsActive.HasValue)
            existing.IsActive = model.IsActive.Value;
        existing.CommitmentType = model.CommitmentType ?? existing.CommitmentType;
        existing.PaymentSource = model.PaymentSource ?? existing.PaymentSource;
        existing.BillingDay = model.BillingDay ?? existing.BillingDay;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static ActiveCommitmentModel ToActiveModel(this FixedCommitmentEntity entity, string installmentStatus, string targetCycle)
    {
        var isPaid = string.Equals(entity.LastPaidCycle, targetCycle, StringComparison.OrdinalIgnoreCase);
        return new ActiveCommitmentModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            Title = entity.Title,
            Amount = entity.Amount,
            Badge = installmentStatus,
            InstallmentStatus = installmentStatus,
            CommitmentType = entity.CommitmentType,
            PaymentSource = entity.PaymentSource,
            BillingDay = entity.BillingDay,
            LastPaidCycle = entity.LastPaidCycle,
            IsPaidInCycle = isPaid,
            IsPaidForCycle = isPaid
        };
    }
}
