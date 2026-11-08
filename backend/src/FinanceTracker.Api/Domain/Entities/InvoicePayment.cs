namespace FinanceTracker.Api.Domain.Entities;

public class InvoicePayment
{
    public int Id { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = true;
    public decimal PaidAmount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Now;
    public decimal SurplusAmount { get; set; }
    public bool SurplusInvested { get; set; } = false;
}
