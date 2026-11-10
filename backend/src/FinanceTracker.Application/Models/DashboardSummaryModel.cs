namespace FinanceTracker.Application.Models;

public class DashboardSummaryModel
{
    public string BillingCycle { get; set; } = string.Empty;
    public string CurrentActiveCycle { get; set; } = string.Empty;
    public string CurrentBillingCycle { get; set; } = string.Empty;
    public DateTime CycleClosingDate { get; set; }
    public DateTime CycleDueDate { get; set; }
    public string CycleState { get; set; } = "CURRENT";
    public int DaysUntilClosing { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal IncomeFirstTranche { get; set; }
    public decimal IncomeSecondTranche { get; set; }
    public decimal MonthlyInvestmentGoal { get; set; }

    public decimal CreditCardLimit { get; set; } = 2500.00m;
    public decimal? CustomCardCap { get; set; }
    public decimal AvailableBankCreditLimit { get; set; }
    public decimal TotalUnpaidCardUsage { get; set; }
    public decimal CreditCardBankLimit { get; set; }
    public decimal TotalInstallmentsOnCard { get; set; }
    public decimal BankLimitAvailable { get; set; }
    public decimal? CustomCardCapOverride { get; set; }

    public List<ActiveCommitmentModel> ActiveFixedCommitments { get; set; } = new();
    public decimal TotalFixedCommitments { get; set; }
    public decimal FixedCommitmentsTotal { get; set; }

    public List<ActiveCommitmentModel> ActiveSubscriptions { get; set; } = new();
    public decimal CardSubscriptionsTotal { get; set; }

    public decimal CardHardCap { get; set; }
    public decimal CardHardCapFormula { get; set; }
    public decimal MonthlySpendingCap { get; set; }

    public decimal TotalSpentInCycle { get; set; }
    public decimal CurrentInvoiceTotal { get; set; }
    public decimal PixCaixinhaUsedInCycle { get; set; }
    public decimal RemainingBudget { get; set; }
    public decimal AvailablePerDay { get; set; }

    public string Status { get; set; } = "HEALTHY";
    public decimal CdiAnnualRate { get; set; }
    public decimal PhantomYieldEstimate { get; set; }
    public decimal PhantomYieldFullCap { get; set; }

    // Nubank Caixinha (115% CDI) Tracker
    public decimal NubankCaixinhaBalance { get; set; }
    public decimal NubankCaixinhaCdiRate { get; set; }
    public decimal NubankCaixinhaEffectiveAnnualRate { get; set; }
    public decimal NubankCaixinhaMonthlyYield { get; set; }
    public decimal NubankCaixinhaDailyYield { get; set; }
    public decimal CaixinhaInvestedBalance { get; set; }
    public decimal CaixinhaMonthlyYield { get; set; }
    public decimal CaixinhaDailyYield { get; set; }
    public decimal ProjectedBalanceWithGoal { get; set; }

    // Spending Pace Tracker
    public int DaysElapsed { get; set; }
    public int TotalDaysInCycle { get; set; }
    public decimal IdealPaceAmount { get; set; }
    public string PaceStatus { get; set; } = "ON_TRACK";
    public decimal PaceDifference { get; set; }
    public decimal BurnPacePercent { get; set; }
    public decimal IdealBurnPacePercent { get; set; }

    // Invoice Paid & 2nd Tranche Salary Status
    public bool IsInvoicePaid { get; set; }
    public DateTime? InvoicePaidAt { get; set; }
    public decimal InvoicePaidAmount { get; set; }
    public decimal SecondTrancheAmount { get; set; } = 1000.00m;
    public decimal SecondTrancheSurplus { get; set; }
    public decimal InvoiceSurplusAmount { get; set; }
    public bool IsSurplusInvested { get; set; }

    public List<CategoryBreakdownModel> CategoryBreakdown { get; set; } = new();
    public List<ExpenseItemModel> RecentExpenses { get; set; } = new();
    public List<ActiveCommitmentModel> ActiveCommitments { get; set; } = new();
    public List<CycleDailyBurnModel> DailyBurnHistory { get; set; } = new();
    public List<FutureCycleProjectionModel> FutureProjections { get; set; } = new();
}
