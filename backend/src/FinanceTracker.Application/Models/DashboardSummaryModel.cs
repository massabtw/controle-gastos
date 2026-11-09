namespace FinanceTracker.Application.Models;

public class DashboardSummaryModel
{
    public string CurrentBillingCycle { get; set; } = string.Empty;
    public DateTime CycleClosingDate { get; set; }
    public DateTime CycleDueDate { get; set; }
    public int DaysUntilClosing { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal MonthlyInvestmentGoal { get; set; }
    public decimal FixedCommitmentsTotal { get; set; }
    public decimal MonthlySpendingCap { get; set; }

    public decimal CreditCardBankLimit { get; set; }
    public decimal TotalInstallmentsOnCard { get; set; }
    public decimal BankLimitAvailable { get; set; }
    public decimal? CustomCardCapOverride { get; set; }

    public decimal TotalSpentInCycle { get; set; }
    public decimal CurrentInvoiceTotal { get; set; }
    public decimal PixCaixinhaUsedInCycle { get; set; }
    public decimal RemainingBudget { get; set; }
    public decimal AvailablePerDay { get; set; }
    public decimal BurnPacePercent { get; set; }
    public decimal IdealBurnPacePercent { get; set; }

    public decimal CaixinhaInvestedBalance { get; set; }
    public decimal CaixinhaMonthlyYield { get; set; }
    public decimal CaixinhaDailyYield { get; set; }
    public decimal ProjectedBalanceWithGoal { get; set; }

    public bool IsInvoicePaid { get; set; }
    public decimal InvoicePaidAmount { get; set; }
    public decimal InvoiceSurplusAmount { get; set; }
    public bool IsSurplusInvested { get; set; }
    public DateTime? InvoicePaidAt { get; set; }

    public List<ExpenseItemModel> RecentExpenses { get; set; } = new();
    public List<ActiveCommitmentModel> ActiveCommitments { get; set; } = new();
    public List<CycleDailyBurnModel> DailyBurnHistory { get; set; } = new();
    public List<FutureCycleProjectionModel> FutureProjections { get; set; } = new();
}
