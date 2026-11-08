namespace FinanceTracker.Api.Domain.Dtos;

public class CreateExpenseDto
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal TotalAmount { get; set; }
    public int Installments { get; set; } = 1;
    public DateTime? TransactionDate { get; set; }
    public string PaymentMethod { get; set; } = "CARD"; // "CARD", "PIX_CAIXINHA", "PIX_ACCOUNT"
    public bool DeductFromCycleCap { get; set; } = true;
}

public class UpdateExpenseDto
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public bool UpdateAllGroupInstallments { get; set; } = false;
}

public class ActiveCommitmentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Badge { get; set; } = string.Empty; // e.g. "1/5" or "Fixo"
    public string CommitmentType { get; set; } = "PIX_COMMITMENT"; // "PIX_COMMITMENT" or "SUBSCRIPTION"
    public string PaymentSource { get; set; } = "PIX_CAIXINHA";    // "CARD", "PIX_CAIXINHA", "PIX_ACCOUNT"
    public int? BillingDay { get; set; } = 1;
    public string? LastPaidCycle { get; set; }
    public bool IsPaidInCycle { get; set; } = false;
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardSummaryDto
{
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime CycleClosingDate { get; set; }
    public DateTime CycleDueDate { get; set; }
    public string CycleState { get; set; } = "CURRENT"; // "CURRENT", "FUTURE", "CLOSED"

    public decimal TotalIncome { get; set; }
    public decimal IncomeFirstTranche { get; set; }
    public decimal IncomeSecondTranche { get; set; }
    public decimal MonthlyInvestmentGoal { get; set; }

    public decimal CreditCardLimit { get; set; } = 2500.00m;
    public decimal? CustomCardCap { get; set; }
    public decimal AvailableBankCreditLimit { get; set; }

    public List<ActiveCommitmentDto> ActiveFixedCommitments { get; set; } = new();
    public decimal TotalFixedCommitments { get; set; }

    public List<ActiveCommitmentDto> ActiveSubscriptions { get; set; } = new();
    public decimal CardSubscriptionsTotal { get; set; }

    public decimal CardHardCap { get; set; }
    public decimal CardHardCapFormula { get; set; }
    public decimal CurrentInvoiceTotal { get; set; }
    public decimal PixCaixinhaUsedInCycle { get; set; }
    public decimal RemainingBudget { get; set; }
    public int DaysUntilClosing { get; set; }
    public decimal AvailablePerDay { get; set; }

    public string Status { get; set; } = "HEALTHY"; // "HEALTHY", "WARNING", "INVADING_INVESTMENT"
    public decimal CdiAnnualRate { get; set; }
    public decimal PhantomYieldEstimate { get; set; }
    public decimal PhantomYieldFullCap { get; set; }

    // Nubank Caixinha (115% CDI) Tracker
    public decimal NubankCaixinhaBalance { get; set; }
    public decimal NubankCaixinhaCdiRate { get; set; }
    public decimal NubankCaixinhaEffectiveAnnualRate { get; set; }
    public decimal NubankCaixinhaMonthlyYield { get; set; }
    public decimal NubankCaixinhaDailyYield { get; set; }

    // Spending Pace Tracker
    public int DaysElapsed { get; set; }
    public int TotalDaysInCycle { get; set; }
    public decimal IdealPaceAmount { get; set; }
    public string PaceStatus { get; set; } = "ON_TRACK"; // "AHEAD", "ON_TRACK", "OVERPACED"
    public decimal PaceDifference { get; set; }

    // Invoice Paid & 2nd Tranche Salary Status
    public bool IsInvoicePaid { get; set; } = false;
    public DateTime? InvoicePaidAt { get; set; }
    public decimal InvoicePaidAmount { get; set; }
    public decimal SecondTrancheAmount { get; set; } = 1000.00m;
    public decimal SecondTrancheSurplus { get; set; }
    public bool IsSurplusInvested { get; set; } = false;

    public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
}

public class SyncInvoiceDto
{
    public decimal TargetTotal { get; set; }
    public string? Cycle { get; set; }
}

public class UpdateCaixinhaDto
{
    public decimal Balance { get; set; }
}

public class UpdateCardLimitDto
{
    public decimal CreditCardLimit { get; set; }
    public decimal? CustomCardCap { get; set; }
}

public class ProjectionCycleDto
{
    public string BillingCycle { get; set; } = string.Empty;
    public decimal FixedCommitmentsTotal { get; set; }
    public decimal CommittedCardInstallments { get; set; }
    public decimal ProjectedFreeCap { get; set; }
    public decimal ProjectedDailyBurnRate { get; set; }
}

public class SimulatorRequestDto
{
    public decimal Amount { get; set; }
    public int Installments { get; set; } = 1;
}

public class SimulatorCycleImpactDto
{
    public string BillingCycle { get; set; } = string.Empty;
    public decimal ProjectedFreeCapBefore { get; set; }
    public decimal ProjectedFreeCapAfter { get; set; }
    public decimal InstallmentAmount { get; set; }
    public bool WillInvade { get; set; }
}

public class SimulatorResponseDto
{
    public decimal Amount { get; set; }
    public int Installments { get; set; }
    public decimal InstallmentValue { get; set; }
    public decimal CurrentDailyBefore { get; set; }
    public decimal CurrentDailyAfter { get; set; }
    public decimal RemainingBudgetBefore { get; set; }
    public decimal RemainingBudgetAfter { get; set; }
    public bool WillInvadeInvestmentGoal { get; set; }
    public decimal InvadedAmount { get; set; }
    public List<SimulatorCycleImpactDto> AffectedCycles { get; set; } = new();
}

public class ChatCommandRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class ChatCommandResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = "UNKNOWN";
    public DashboardSummaryDto? Dashboard { get; set; }
}

public class CreatePixCaixinhaDto
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal Amount { get; set; }
    public bool DeductFromCycleCap { get; set; } = true;
    public DateTime? TransactionDate { get; set; }
}

public class DepositCaixinhaDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class ReceivePixInflowDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Category { get; set; } = "Entrada";
    public DateTime? TransactionDate { get; set; }
}
