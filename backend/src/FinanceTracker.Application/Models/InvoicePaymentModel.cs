namespace FinanceTracker.Application.Models;

public class InvoicePaymentModel
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime PaidAt { get; set; }
    public decimal SurplusAmount { get; set; }
    public bool SurplusInvested { get; set; }
}
