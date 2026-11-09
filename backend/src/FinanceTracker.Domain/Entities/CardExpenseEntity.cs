namespace FinanceTracker.Domain.Entities;

public class CardExpenseEntity : Entity
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string BillingCycle { get; set; } = string.Empty;
    public int InstallmentCurrent { get; set; } = 1;
    public int InstallmentTotal { get; set; } = 1;
    public Guid? PurchaseGroupId { get; set; }
    public string PaymentMethod { get; set; } = "CARD";
    public bool DeductFromCycleCap { get; set; } = true;
}
