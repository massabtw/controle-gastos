namespace FinanceTracker.Api.Domain.Entities;

public class FinancialProfile
{
    public int Id { get; set; }
    public decimal IncomeFirstTranche { get; set; } = 1200.00m;
    public decimal IncomeSecondTranche { get; set; } = 1000.00m;
    public decimal MonthlyInvestmentGoal { get; set; } = 1000.00m;
    public int ClosingDay { get; set; } = 8;
    public int DueDay { get; set; } = 16;
    public decimal CdiAnnualRate { get; set; } = 11.25m;

    public decimal TotalIncome => IncomeFirstTranche + IncomeSecondTranche;
}
