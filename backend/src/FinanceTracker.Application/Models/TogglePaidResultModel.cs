namespace FinanceTracker.Application.Models;

public class TogglePaidResultModel
{
    public string Message { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public string Cycle { get; set; } = string.Empty;
    public string TargetCycle { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public decimal NewCaixinhaBalance { get; set; }
}
