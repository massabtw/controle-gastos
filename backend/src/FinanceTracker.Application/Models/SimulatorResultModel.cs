namespace FinanceTracker.Application.Models;

public class SimulatorResultModel
{
    public decimal Amount { get; set; }
    public decimal TotalAmount { get; set; }
    public int Installments { get; set; }
    public decimal InstallmentValue { get; set; }
    public decimal CurrentDailyBefore { get; set; }
    public decimal CurrentDailyAfter { get; set; }
    public decimal RemainingBudgetBefore { get; set; }
    public decimal RemainingBudgetAfter { get; set; }
    public bool WillInvadeInvestmentGoal { get; set; }
    public decimal InvadedAmount { get; set; }
    public List<SimulatorMonthImpactModel> AffectedCycles { get; set; } = new();

    public bool IsViable { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public decimal CurrentCaixinhaYield { get; set; }
    public List<SimulatorMonthImpactModel> MonthlyImpacts { get; set; } = new();
}
