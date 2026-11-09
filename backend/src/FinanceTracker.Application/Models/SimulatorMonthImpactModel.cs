namespace FinanceTracker.Application.Models;

public class SimulatorMonthImpactModel
{
    public string Cycle { get; set; } = string.Empty;
    public decimal MonthlySpendingCap { get; set; }
    public decimal AlreadyCommittedInstallments { get; set; }
    public decimal FixedCommitments { get; set; }
    public decimal SimulatedInstallment { get; set; }
    public decimal TotalExpectedCommitment { get; set; }
    public decimal RemainingAvailableCap { get; set; }
    public bool ExceedsCap { get; set; }
}
