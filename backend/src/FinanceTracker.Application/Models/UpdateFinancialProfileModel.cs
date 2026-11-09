namespace FinanceTracker.Application.Models;

public class UpdateFinancialProfileModel
{
    public decimal IncomeFirstTranche { get; set; }
    public decimal IncomeSecondTranche { get; set; }
    public decimal MonthlyInvestmentGoal { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal CdiAnnualRate { get; set; }
    public decimal NubankCaixinhaBalance { get; set; }
    public decimal NubankCaixinhaCdiRate { get; set; }
    public decimal CreditCardLimit { get; set; }
    public decimal? CustomCardCap { get; set; }
}
