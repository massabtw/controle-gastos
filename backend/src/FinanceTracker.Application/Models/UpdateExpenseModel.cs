namespace FinanceTracker.Application.Models;

public class UpdateExpenseModel
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public bool UpdateAllGroupInstallments { get; set; }
}
