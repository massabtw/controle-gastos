namespace FinanceTracker.Application.Models;

public class ExpenseItemModel
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public int InstallmentCurrent { get; set; }
    public int InstallmentTotal { get; set; }
    public Guid? PurchaseGroupId { get; set; }
    public string PaymentMethod { get; set; } = "CARD";
    public bool DeductFromCycleCap { get; set; } = true;
}
