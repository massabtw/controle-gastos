namespace FinanceTracker.Application.Models;

public class IosShortcutResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ExpenseItemModel? Expense { get; set; }
    public decimal DailyBurnRate { get; set; }
    public decimal RemainingCap { get; set; }
}
