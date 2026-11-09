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
        return new FixedCommitmentEntity
        {
            Title = model.Title,
            Amount = model.Amount,
            StartCycle = !string.IsNullOrWhiteSpace(model.StartCycle) ? model.StartCycle : startCycle,
            TotalInstallments = model.TotalInstallments,
            IsActive = true,
            CommitmentType = string.IsNullOrWhiteSpace(model.CommitmentType) ? "PIX_COMMITMENT" : model.CommitmentType,
            PaymentSource = string.IsNullOrWhiteSpace(model.PaymentSource) ? "PIX_CAIXINHA" : model.PaymentSource,
            BillingDay = model.BillingDay ?? 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Deleted = false
        };
    }

    public static ActiveCommitmentModel ToActiveModel(this FixedCommitmentEntity entity, string installmentStatus, string targetCycle)
    {
        return new ActiveCommitmentModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            Title = entity.Title,
            Amount = entity.Amount,
            InstallmentStatus = installmentStatus,
            CommitmentType = entity.CommitmentType,
            PaymentSource = entity.PaymentSource,
            BillingDay = entity.BillingDay,
            LastPaidCycle = entity.LastPaidCycle,
            IsPaidForCycle = entity.LastPaidCycle == targetCycle
        };
    }
}
