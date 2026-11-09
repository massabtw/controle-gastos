namespace FinanceTracker.Application.Models;

public class FutureCycleProjectionModel
{
    public string BillingCycle { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public decimal FixedCommitmentsTotal { get; set; }
    public decimal FixedCommitments { get; set; }
    public decimal CommittedCardInstallments { get; set; }
    public decimal CommittedInstallments { get; set; }
    public decimal ProjectedFreeCap { get; set; }
    public decimal ProjectedDailyBurnRate { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal SafeSpendingCap { get; set; }
    public decimal RemainingAvailable { get; set; }
}
