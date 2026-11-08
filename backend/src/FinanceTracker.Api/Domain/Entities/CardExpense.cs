namespace FinanceTracker.Api.Domain.Entities;

public class CardExpense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string BillingCycle { get; set; } = string.Empty; // Format: 'YYYY-MM'
    public int InstallmentCurrent { get; set; } = 1;
    public int InstallmentTotal { get; set; } = 1;
    public Guid? PurchaseGroupId { get; set; }
    public string PaymentMethod { get; set; } = "CARD"; // "CARD" | "PIX_CAIXINHA"
    public bool DeductFromCycleCap { get; set; } = true;
}
