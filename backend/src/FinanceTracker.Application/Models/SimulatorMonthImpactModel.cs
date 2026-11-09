namespace FinanceTracker.Application.Models;

public class SimulatorMonthImpactModel
{
    public string BillingCycle { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public decimal ProjectedFreeCapBefore { get; set; }
    public decimal ProjectedFreeCapAfter { get; set; }
    public decimal InstallmentAmount { get; set; }
    public bool WillInvade { get; set; }

    public decimal MonthlySpendingCap { get; set; }
    public decimal AlreadyCommittedInstallments { get; set; }
    public decimal FixedCommitments { get; set; }
    public decimal SimulatedInstallment { get; set; }
    public decimal TotalExpectedCommitment { get; set; }
    public decimal RemainingAvailableCap { get; set; }
    public bool ExceedsCap { get; set; }
}
