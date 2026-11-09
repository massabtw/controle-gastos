namespace FinanceTracker.Domain.Entities;

public class InvoicePaymentEntity : Entity
{
    public string BillingCycle { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = true;
    public decimal PaidAmount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public decimal SurplusAmount { get; set; }
    public bool SurplusInvested { get; set; }
}
