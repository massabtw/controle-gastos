namespace FinanceTracker.Application.Models;

public class CategoryBreakdownModel
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}
