namespace FinanceTracker.Application.Models;

public class FutureCycleProjectionModel
{
    public string Cycle { get; set; } = string.Empty;
    public decimal CommittedInstallments { get; set; }
    public decimal FixedCommitments { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal SafeSpendingCap { get; set; }
    public decimal RemainingAvailable { get; set; }
}
