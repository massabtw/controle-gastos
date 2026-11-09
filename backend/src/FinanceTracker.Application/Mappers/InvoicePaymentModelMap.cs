using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Mappers;

public static class InvoicePaymentModelMap
{
    public static InvoicePaymentModel ToModel(this InvoicePaymentEntity entity)
    {
        return new InvoicePaymentModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            BillingCycle = entity.BillingCycle,
            IsPaid = entity.IsPaid,
            PaidAmount = entity.PaidAmount,
            PaidAt = entity.PaidAt,
            SurplusAmount = entity.SurplusAmount,
            SurplusInvested = entity.SurplusInvested
        };
    }
}
