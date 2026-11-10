using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.Services;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.FinancialEngine;

public class FinancialEngineService : IFinancialEngineService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly ILogger<FinancialEngineService> _logger;

    public FinancialEngineService(
        IUnitOfWorkApp unitOfWork,
        ILogger<FinancialEngineService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DashboardSummaryModel>> GetDashboardSummaryAsync(string? targetCycle = null)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var now = DateTime.Now;

            var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(now, profile.ClosingDay);
            var inspectedCycle = string.IsNullOrWhiteSpace(targetCycle) ? currentActiveCycle : targetCycle;

            var (closingDate, dueDate) = BillingCycleCalculator.GetCycleDates(inspectedCycle, profile.ClosingDay, profile.DueDay);
            var (cycleStartDate, cycleEndDate) = BillingCycleCalculator.GetCyclePeriod(inspectedCycle, profile.ClosingDay);

            var cycleComparison = string.CompareOrdinal(inspectedCycle, currentActiveCycle);
            var cycleState = cycleComparison < 0 ? "CLOSED" : (cycleComparison > 0 ? "FUTURE" : "CURRENT");

            var allCommitments = (await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true)).ToList();
            var activeFixedCommitmentsList = new List<ActiveCommitmentModel>();
            var activeSubscriptionsList = new List<ActiveCommitmentModel>();
            var activeCommitmentsMerged = new List<ActiveCommitmentModel>();
            decimal totalFixedCommitments = 0m;
            decimal cardSubscriptionsTotal = 0m;

            foreach (var commitment in allCommitments)
            {
                var isSubscription = string.Equals(commitment.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase);

                if (isSubscription)
                {
                    // Subscriptions only take effect from their StartCycle onwards.
                    if (!string.IsNullOrWhiteSpace(commitment.StartCycle) &&
                        string.CompareOrdinal(inspectedCycle, commitment.StartCycle) < 0)
                    {
                        continue;
                    }

                    var subModel = commitment.ToActiveModel("Mensal", inspectedCycle);
                    subModel.CommitmentType = "SUBSCRIPTION";
                    subModel.PaymentSource = string.IsNullOrWhiteSpace(commitment.PaymentSource) ? "CARD" : commitment.PaymentSource;
                    activeSubscriptionsList.Add(subModel);
                    activeCommitmentsMerged.Add(subModel);

                    if (string.Equals(subModel.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase))
                    {
                        cardSubscriptionsTotal += commitment.Amount;
                    }
                }
                else
                {
                    var badge = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, inspectedCycle);
                    if (badge != null)
                    {
                        var fixedModel = commitment.ToActiveModel(badge, inspectedCycle);
                        fixedModel.CommitmentType = "PIX_COMMITMENT";
                        fixedModel.PaymentSource = string.IsNullOrWhiteSpace(commitment.PaymentSource) ? "PIX_CAIXINHA" : commitment.PaymentSource;
                        activeFixedCommitmentsList.Add(fixedModel);
                        activeCommitmentsMerged.Add(fixedModel);
                        totalFixedCommitments += commitment.Amount;
                    }
                }
            }

            // Hard Cap Formula: TotalIncome - InvestmentGoal - TotalFixedCommitments
            var totalIncome = profile.TotalIncome;
            var formulaHardCap = totalIncome - profile.MonthlyInvestmentGoal - totalFixedCommitments;
            var cardHardCap = (profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0)
                ? profile.CustomCardCap.Value
                : formulaHardCap;

            // Expenses in target cycle
            var cycleExpensesEntities = (await _unitOfWork.CardExpense.GetByCycleAsync(inspectedCycle)).ToList();
            var manualCardExpensesTotal = cycleExpensesEntities.Where(e => e.PaymentMethod == "CARD").Sum(e => e.Amount);
            var currentInvoiceTotal = manualCardExpensesTotal + cardSubscriptionsTotal;
            var pixCaixinhaUsedInCycle = cycleExpensesEntities.Where(e => e.PaymentMethod == "PIX_CAIXINHA" && e.DeductFromCycleCap).Sum(e => e.Amount);
            var otherDeductibleExpenses = cycleExpensesEntities.Where(e => e.PaymentMethod != "CARD" && e.PaymentMethod != "PIX_CAIXINHA" && e.PaymentMethod != "PIX_INFLOW" && e.DeductFromCycleCap).Sum(e => e.Amount);
            var totalSpentInCycle = currentInvoiceTotal + pixCaixinhaUsedInCycle + otherDeductibleExpenses;
            var remainingBudget = cardHardCap - totalSpentInCycle;

            var daysUntilClosing = BillingCycleCalculator.GetDaysUntilClosing(now, profile.ClosingDay, inspectedCycle);
            var availablePerDay = (daysUntilClosing > 0 && remainingBudget > 0)
                ? Math.Round(remainingBudget / daysUntilClosing, 2)
                : 0m;

            // Status calculation
            string status;
            if (remainingBudget < 0)
            {
                status = "INVADING_INVESTMENT";
            }
            else if (availablePerDay < 20m && cycleState == "CURRENT")
            {
                status = "WARNING";
            }
            else
            {
                status = "HEALTHY";
            }

            // CDI Yield: Monthly CDI rate = (CdiAnnualRate / 100) / 12
            var monthlyCdiRate = (profile.CdiAnnualRate / 100m) / 12m;
            var phantomYieldEstimate = Math.Round(currentInvoiceTotal * monthlyCdiRate, 2);
            var phantomYieldFullCap = Math.Round(Math.Max(0, cardHardCap) * monthlyCdiRate, 2);

            // Nubank Caixinha (115% CDI) Yield
            var caixinhaCdiRate = profile.NubankCaixinhaCdiRate > 0 ? profile.NubankCaixinhaCdiRate : 115.00m;
            var caixinhaEffectiveAnnual = Math.Round(profile.CdiAnnualRate * (caixinhaCdiRate / 100m), 2);
            var caixinhaMonthlyRate = (caixinhaEffectiveAnnual / 100m) / 12m;
            var caixinhaMonthlyYield = Math.Round(profile.NubankCaixinhaBalance * caixinhaMonthlyRate, 2);
            var caixinhaDailyYield = Math.Round(caixinhaMonthlyYield / 30m, 2);
            var projectedBalanceWithGoal = profile.NubankCaixinhaBalance + profile.MonthlyInvestmentGoal + caixinhaMonthlyYield;

            // Spending Pace calculation
            var totalDaysInCycle = Math.Max(1, (cycleEndDate.Date - cycleStartDate.Date).Days);
            var daysElapsed = Math.Clamp((now.Date - cycleStartDate.Date).Days, 0, totalDaysInCycle);
            var idealDailyBudget = totalDaysInCycle > 0 ? (cardHardCap / totalDaysInCycle) : 0m;
            var idealPaceAmount = Math.Round(idealDailyBudget * daysElapsed, 2);
            var paceDiff = currentInvoiceTotal - idealPaceAmount;
            var paceStatus = paceDiff < -50m ? "AHEAD" : (paceDiff > 50m ? "OVERPACED" : "ON_TRACK");
            var idealBurnPacePercent = (decimal)daysElapsed / totalDaysInCycle * 100.0m;
            var burnPacePercent = cardHardCap > 0 ? (totalSpentInCycle / cardHardCap) * 100.0m : 0m;

            // Invoice Paid & 2nd Tranche Salary Status
            var invoicePayment = await _unitOfWork.InvoicePayment.GetByCycleAsync(inspectedCycle);
            var isInvoicePaid = invoicePayment != null && invoicePayment.IsPaid;
            var invoicePaidAmount = (isInvoicePaid && invoicePayment != null) ? invoicePayment.PaidAmount : 0m;
            var invoicePaidAt = (isInvoicePaid && invoicePayment != null) ? invoicePayment.PaidAt : (DateTime?)null;
            var secondTrancheAmount = profile.IncomeSecondTranche;
            var secondTrancheSurplus = (isInvoicePaid && invoicePayment != null)
                ? invoicePayment.SurplusAmount
                : Math.Max(0, secondTrancheAmount - currentInvoiceTotal);
            var isSurplusInvested = isInvoicePaid && (invoicePayment?.SurplusInvested ?? false);

            // Available Bank Credit Limit (Global across all billing cycles)
            var bankLimit = profile.CreditCardLimit > 0 ? profile.CreditCardLimit : 2500.00m;
            var totalUnpaidCardUsage = await _unitOfWork.CardExpense.GetTotalUnpaidCardExpensesAsync();
            var totalFutureInstallmentsOnCard = await _unitOfWork.CardExpense.GetFutureCommittedInstallmentsTotalAsync(inspectedCycle);
            var availableBankCreditLimit = Math.Max(0, bankLimit - totalUnpaidCardUsage);

            // Category breakdown (include subscriptions category)
            var allCardItems = cycleExpensesEntities
                .Where(e => e.PaymentMethod != "PIX_INFLOW")
                .Select(e => new { e.Category, e.Amount })
                .ToList();

            foreach (var sub in activeSubscriptionsList.Where(s => s.PaymentSource == "CARD"))
            {
                allCardItems.Add(new { Category = "Assinaturas", sub.Amount });
            }

            var categoryGroups = allCardItems
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Outros" : e.Category)
                .Select(g => new CategoryBreakdownModel
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Percentage = currentInvoiceTotal > 0 ? Math.Round((g.Sum(e => e.Amount) / currentInvoiceTotal) * 100m, 1) : 0m
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var dailyBurnHistory = cycleExpensesEntities
                .Where(e => e.PaymentMethod != "PIX_INFLOW")
                .GroupBy(e => e.TransactionDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new CycleDailyBurnModel
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.Amount)
                })
                .ToList();

            var projectionsResult = await GetProjectionsAsync(6);
            var futureProjections = projectionsResult.IsSuccess && projectionsResult.Value != null
                ? projectionsResult.Value.ToList()
                : new List<FutureCycleProjectionModel>();

            var recentEntities = (await _unitOfWork.CardExpense.GetRecentAsync(15)).ToList();

            return new DashboardSummaryModel
            {
                BillingCycle = inspectedCycle,
                CurrentActiveCycle = currentActiveCycle,
                CurrentBillingCycle = inspectedCycle,
                CycleClosingDate = closingDate,
                CycleDueDate = dueDate,
                CycleState = cycleState,
                DaysUntilClosing = daysUntilClosing,

                TotalIncome = totalIncome,
                IncomeFirstTranche = profile.IncomeFirstTranche,
                IncomeSecondTranche = profile.IncomeSecondTranche,
                MonthlyInvestmentGoal = profile.MonthlyInvestmentGoal,

                CreditCardLimit = bankLimit,
                CustomCardCap = profile.CustomCardCap,
                AvailableBankCreditLimit = availableBankCreditLimit,
                TotalUnpaidCardUsage = totalUnpaidCardUsage,
                CreditCardBankLimit = bankLimit,
                TotalInstallmentsOnCard = totalFutureInstallmentsOnCard,
                BankLimitAvailable = availableBankCreditLimit,
                CustomCardCapOverride = profile.CustomCardCap,

                ActiveFixedCommitments = activeFixedCommitmentsList,
                TotalFixedCommitments = totalFixedCommitments,
                FixedCommitmentsTotal = totalFixedCommitments,

                ActiveSubscriptions = activeSubscriptionsList,
                CardSubscriptionsTotal = cardSubscriptionsTotal,

                CardHardCap = cardHardCap,
                CardHardCapFormula = formulaHardCap,
                MonthlySpendingCap = cardHardCap,

                TotalSpentInCycle = totalSpentInCycle,
                CurrentInvoiceTotal = currentInvoiceTotal,
                PixCaixinhaUsedInCycle = pixCaixinhaUsedInCycle,
                RemainingBudget = remainingBudget,
                AvailablePerDay = availablePerDay,

                Status = status,
                CdiAnnualRate = profile.CdiAnnualRate,
                PhantomYieldEstimate = phantomYieldEstimate,
                PhantomYieldFullCap = phantomYieldFullCap,

                NubankCaixinhaBalance = profile.NubankCaixinhaBalance,
                NubankCaixinhaCdiRate = caixinhaCdiRate,
                NubankCaixinhaEffectiveAnnualRate = caixinhaEffectiveAnnual,
                NubankCaixinhaMonthlyYield = caixinhaMonthlyYield,
                NubankCaixinhaDailyYield = caixinhaDailyYield,
                CaixinhaInvestedBalance = profile.NubankCaixinhaBalance,
                CaixinhaMonthlyYield = caixinhaMonthlyYield,
                CaixinhaDailyYield = caixinhaDailyYield,
                ProjectedBalanceWithGoal = projectedBalanceWithGoal,

                DaysElapsed = daysElapsed,
                TotalDaysInCycle = totalDaysInCycle,
                IdealPaceAmount = idealPaceAmount,
                PaceStatus = paceStatus,
                PaceDifference = paceDiff,
                BurnPacePercent = burnPacePercent,
                IdealBurnPacePercent = idealBurnPacePercent,

                IsInvoicePaid = isInvoicePaid,
                InvoicePaidAt = invoicePaidAt,
                InvoicePaidAmount = invoicePaidAmount,
                SecondTrancheAmount = secondTrancheAmount,
                SecondTrancheSurplus = secondTrancheSurplus,
                InvoiceSurplusAmount = secondTrancheSurplus,
                IsSurplusInvested = isSurplusInvested,

                CategoryBreakdown = categoryGroups,
                RecentExpenses = recentEntities.ToModel().ToList(),
                ActiveCommitments = activeCommitmentsMerged,
                DailyBurnHistory = dailyBurnHistory,
                FutureProjections = futureProjections
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard summary for targetCycle: {TargetCycle}", targetCycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<IEnumerable<FutureCycleProjectionModel>>> GetProjectionsAsync(int months = 6)
    {
        try
        {
            var clampedMonths = Math.Clamp(months, 1, 24);
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
            var allCommitments = (await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true)).ToList();

            var cycles = new List<string>();
            for (var i = 0; i < clampedMonths; i++)
            {
                cycles.Add(BillingCycleCalculator.AddMonthsToCycle(currentActiveCycle, i));
            }

            var allExpensesInCycles = (await _unitOfWork.CardExpense.GetExpensesInCyclesAsync(cycles)).ToList();
            var projections = new List<FutureCycleProjectionModel>();

            foreach (var cycle in cycles)
            {
                decimal fixedCommitmentsTotal = 0m;
                foreach (var commitment in allCommitments.Where(c => !string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase)))
                {
                    var badge = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, cycle);
                    if (badge != null)
                    {
                        fixedCommitmentsTotal += commitment.Amount;
                    }
                }

                var cardSubscriptionsInCycle = allCommitments
                    .Where(c => string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(c.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase) &&
                                (string.IsNullOrWhiteSpace(c.StartCycle) || string.CompareOrdinal(cycle, c.StartCycle) >= 0))
                    .Sum(c => c.Amount);

                var cardInstallments = allExpensesInCycles
                    .Where(e => e.BillingCycle == cycle && e.PaymentMethod == "CARD")
                    .Sum(e => e.Amount) + cardSubscriptionsInCycle;

                var baseCap = (profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0)
                    ? profile.CustomCardCap.Value
                    : (profile.TotalIncome - profile.MonthlyInvestmentGoal - fixedCommitmentsTotal);

                var projectedFreeCap = baseCap - cardInstallments;
                var projectedDailyBurnRate = Math.Round(projectedFreeCap / 30m, 2);

                projections.Add(new FutureCycleProjectionModel
                {
                    BillingCycle = cycle,
                    Cycle = cycle,
                    FixedCommitmentsTotal = fixedCommitmentsTotal,
                    FixedCommitments = fixedCommitmentsTotal,
                    CommittedCardInstallments = cardInstallments,
                    CommittedInstallments = cardInstallments,
                    ProjectedFreeCap = projectedFreeCap,
                    ProjectedDailyBurnRate = projectedDailyBurnRate,
                    TotalExpected = cardInstallments,
                    SafeSpendingCap = baseCap,
                    RemainingAvailable = projectedFreeCap
                });
            }

            return Result<IEnumerable<FutureCycleProjectionModel>>.Success(projections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating projections for {Months} months", months);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<SimulatorResultModel>> SimulatePurchaseAsync(SimulatorRequestModel request)
    {
        try
        {
            var installments = Math.Max(1, request.Installments);
            var installmentValue = Math.Round(request.Amount / installments, 2);

            var dashboardResult = await GetDashboardSummaryAsync(request.TargetCycle);
            if (!dashboardResult.IsSuccess || dashboardResult.Value == null)
            {
                return ErrorMessages.UnknownError;
            }

            var dashboard = dashboardResult.Value;
            var currentDailyBefore = dashboard.AvailablePerDay;
            var remainingBudgetBefore = dashboard.RemainingBudget;
            var remainingBudgetAfter = remainingBudgetBefore - installmentValue;

            var willInvade = remainingBudgetAfter < 0;
            var invadedAmount = willInvade ? Math.Abs(remainingBudgetAfter) : 0m;

            var currentDailyAfter = (dashboard.DaysUntilClosing > 0 && remainingBudgetAfter > 0)
                ? Math.Round(remainingBudgetAfter / dashboard.DaysUntilClosing, 2)
                : 0m;

            var projectionsResult = await GetProjectionsAsync(Math.Max(6, installments));
            var projections = projectionsResult.IsSuccess && projectionsResult.Value != null
                ? projectionsResult.Value.ToList()
                : new List<FutureCycleProjectionModel>();

            var affectedCycles = new List<SimulatorMonthImpactModel>();
            var allMonthsViable = true;

            for (var i = 0; i < installments; i++)
            {
                var cycle = BillingCycleCalculator.AddMonthsToCycle(dashboard.BillingCycle, i);
                var proj = projections.FirstOrDefault(p => p.BillingCycle == cycle);
                var freeCapBefore = proj?.ProjectedFreeCap ?? 0m;
                var freeCapAfter = freeCapBefore - installmentValue;
                var cycleWillInvade = freeCapAfter < 0;

                if (cycleWillInvade)
                {
                    allMonthsViable = false;
                }

                affectedCycles.Add(new SimulatorMonthImpactModel
                {
                    BillingCycle = cycle,
                    Cycle = cycle,
                    ProjectedFreeCapBefore = freeCapBefore,
                    ProjectedFreeCapAfter = freeCapAfter,
                    InstallmentAmount = installmentValue,
                    WillInvade = cycleWillInvade,
                    MonthlySpendingCap = proj?.SafeSpendingCap ?? dashboard.CardHardCap,
                    AlreadyCommittedInstallments = proj?.CommittedCardInstallments ?? 0m,
                    FixedCommitments = proj?.FixedCommitmentsTotal ?? dashboard.TotalFixedCommitments,
                    SimulatedInstallment = installmentValue,
                    TotalExpectedCommitment = (proj?.CommittedCardInstallments ?? 0m) + installmentValue,
                    RemainingAvailableCap = freeCapAfter,
                    ExceedsCap = cycleWillInvade
                });
            }

            string recommendation = allMonthsViable
                ? $"Compra viável! A parcela de R$ {installmentValue:F2} cabe no teto de todos os {installments} meses simulados sem comprometer a meta de investimento."
                : $"Atenção: A parcela de R$ {installmentValue:F2} estoura o teto de gastos seguro em um ou mais meses. Considere parcelar em mais vezes ou adiar a compra.";

            return new SimulatorResultModel
            {
                Amount = request.Amount,
                TotalAmount = request.Amount,
                Installments = installments,
                InstallmentValue = installmentValue,
                CurrentDailyBefore = currentDailyBefore,
                CurrentDailyAfter = currentDailyAfter,
                RemainingBudgetBefore = remainingBudgetBefore,
                RemainingBudgetAfter = remainingBudgetAfter,
                WillInvadeInvestmentGoal = willInvade,
                InvadedAmount = invadedAmount,
                AffectedCycles = affectedCycles,
                IsViable = allMonthsViable,
                Recommendation = recommendation,
                CurrentCaixinhaYield = dashboard.NubankCaixinhaMonthlyYield,
                MonthlyImpacts = affectedCycles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating purchase for amount {Amount}", request.Amount);
            return ErrorMessages.UnknownError;
        }
    }
}
