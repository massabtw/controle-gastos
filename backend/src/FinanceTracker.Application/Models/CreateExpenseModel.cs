namespace FinanceTracker.Application.Models;

public class CreateExpenseModel
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal Amount { get; set; }
    public int Installments { get; set; } = 1;
    public DateTime? TransactionDate { get; set; }
    public string? PaymentMethod { get; set; }
    public bool? DeductFromCycleCap { get; set; }
}
