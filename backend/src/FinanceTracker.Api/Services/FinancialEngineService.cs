using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Domain.Services;
using FinanceTracker.Api.Infrastructure.Repositories;

namespace FinanceTracker.Api.Services;

public interface IFinancialEngineService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(string? cycle = null);
    Task<List<ProjectionCycleDto>> GetProjectionsAsync(int months = 6);
    Task<SimulatorResponseDto> SimulatePurchaseAsync(SimulatorRequestDto dto);
}

public class FinancialEngineService : IFinancialEngineService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IExpenseRepository _expenseRepository;

    public FinancialEngineService(IProfileRepository profileRepository, IExpenseRepository expenseRepository)
    {
        _profileRepository = profileRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string? cycle = null)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
        var targetCycle = string.IsNullOrWhiteSpace(cycle) ? currentActiveCycle : cycle;

        var (closingDate, dueDate) = BillingCycleCalculator.GetCycleDates(targetCycle, profile.ClosingDay, profile.DueDay);

        var cycleComparison = string.CompareOrdinal(targetCycle, currentActiveCycle);
        var cycleState = cycleComparison < 0 ? "CLOSED" : (cycleComparison > 0 ? "FUTURE" : "CURRENT");

        // Calculate active commitments and subscriptions for this cycle
        var allCommitments = (await _profileRepository.GetFixedCommitmentsAsync(onlyActive: true)).ToList();
        var activeCommitmentsList = new List<ActiveCommitmentDto>();
        var activeSubscriptionsList = new List<ActiveCommitmentDto>();
        decimal totalFixedCommitments = 0m;
        decimal cardSubscriptionsTotal = 0m;

        foreach (var commitment in allCommitments)
        {
            var isSubscription = string.Equals(commitment.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase);
            var isPaidInCycle = string.Equals(commitment.LastPaidCycle, targetCycle, StringComparison.OrdinalIgnoreCase);

            if (isSubscription)
            {
                activeSubscriptionsList.Add(new ActiveCommitmentDto
                {
                    Id = commitment.Id,
                    Title = commitment.Title,
                    Amount = commitment.Amount,
                    Badge = "Mensal",
                    CommitmentType = "SUBSCRIPTION",
                    PaymentSource = string.IsNullOrWhiteSpace(commitment.PaymentSource) ? "CARD" : commitment.PaymentSource,
                    BillingDay = commitment.BillingDay ?? 1,
                    LastPaidCycle = commitment.LastPaidCycle,
                    IsPaidInCycle = isPaidInCycle
                });

                if (string.Equals(commitment.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase))
                {
                    cardSubscriptionsTotal += commitment.Amount;
                }
            }
            else
            {
                var badge = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, targetCycle);
                if (badge != null)
                {
                    activeCommitmentsList.Add(new ActiveCommitmentDto
                    {
                        Id = commitment.Id,
                        Title = commitment.Title,
                        Amount = commitment.Amount,
                        Badge = badge,
                        CommitmentType = "PIX_COMMITMENT",
                        PaymentSource = string.IsNullOrWhiteSpace(commitment.PaymentSource) ? "PIX_CAIXINHA" : commitment.PaymentSource,
                        BillingDay = commitment.BillingDay ?? 1,
                        LastPaidCycle = commitment.LastPaidCycle,
                        IsPaidInCycle = isPaidInCycle
                    });
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
        var expenses = (await _expenseRepository.GetExpensesByCycleAsync(targetCycle)).ToList();
        var manualCardExpensesTotal = expenses.Where(e => e.PaymentMethod == "CARD").Sum(e => e.Amount);
        var currentInvoiceTotal = manualCardExpensesTotal + cardSubscriptionsTotal;
        var pixCaixinhaUsedInCycle = expenses.Where(e => e.PaymentMethod == "PIX_CAIXINHA" && e.DeductFromCycleCap).Sum(e => e.Amount);
        var remainingBudget = cardHardCap - currentInvoiceTotal - pixCaixinhaUsedInCycle;

        var daysUntilClosing = BillingCycleCalculator.GetDaysUntilClosing(DateTime.Now, profile.ClosingDay, targetCycle);

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

        // Spending Pace calculation
        var (cycleStart, cycleEnd) = BillingCycleCalculator.GetCyclePeriod(targetCycle, profile.ClosingDay);
        var totalDaysInCycle = Math.Max(1, (cycleEnd - cycleStart).Days);
        var daysElapsed = Math.Clamp((DateTime.Now.Date - cycleStart.Date).Days, 0, totalDaysInCycle);
        var idealDailyBudget = totalDaysInCycle > 0 ? (cardHardCap / totalDaysInCycle) : 0m;
        var idealPaceAmount = Math.Round(idealDailyBudget * daysElapsed, 2);
        var paceDiff = currentInvoiceTotal - idealPaceAmount;
        var paceStatus = paceDiff < -50m ? "AHEAD" : (paceDiff > 50m ? "OVERPACED" : "ON_TRACK");

        // Available Bank Credit Limit
        var bankLimit = profile.CreditCardLimit > 0 ? profile.CreditCardLimit : 2500.00m;
        var allCommittedCardExpenses = await _expenseRepository.GetTotalCommittedCardExpensesFromCycleAsync(targetCycle);
        var availableBankCreditLimit = Math.Max(0, bankLimit - allCommittedCardExpenses - cardSubscriptionsTotal);

        // Category breakdown (include subscriptions category)
        var allCardItems = expenses.Select(e => new { e.Category, e.Amount }).ToList();
        foreach (var sub in activeSubscriptionsList.Where(s => s.PaymentSource == "CARD"))
        {
            allCardItems.Add(new { Category = "Assinaturas", sub.Amount });
        }

        var categoryGroups = allCardItems
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Outros" : e.Category)
            .Select(g => new CategoryBreakdownDto
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Percentage = currentInvoiceTotal > 0 ? Math.Round((g.Sum(e => e.Amount) / currentInvoiceTotal) * 100m, 1) : 0m
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new DashboardSummaryDto
        {
            BillingCycle = targetCycle,
            CycleClosingDate = closingDate,
            CycleDueDate = dueDate,
            CycleState = cycleState,

            TotalIncome = totalIncome,
            IncomeFirstTranche = profile.IncomeFirstTranche,
            IncomeSecondTranche = profile.IncomeSecondTranche,
            MonthlyInvestmentGoal = profile.MonthlyInvestmentGoal,

            CreditCardLimit = bankLimit,
            CustomCardCap = profile.CustomCardCap,
            AvailableBankCreditLimit = availableBankCreditLimit,

            ActiveFixedCommitments = activeCommitmentsList,
            TotalFixedCommitments = totalFixedCommitments,

            ActiveSubscriptions = activeSubscriptionsList,
            CardSubscriptionsTotal = cardSubscriptionsTotal,

            CardHardCap = cardHardCap,
            CardHardCapFormula = formulaHardCap,
            CurrentInvoiceTotal = currentInvoiceTotal,
            PixCaixinhaUsedInCycle = pixCaixinhaUsedInCycle,
            RemainingBudget = remainingBudget,
            DaysUntilClosing = daysUntilClosing,
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

            DaysElapsed = daysElapsed,
            TotalDaysInCycle = totalDaysInCycle,
            IdealPaceAmount = idealPaceAmount,
            PaceStatus = paceStatus,
            PaceDifference = paceDiff,

            CategoryBreakdown = categoryGroups
        };
    }

    public async Task<List<ProjectionCycleDto>> GetProjectionsAsync(int months = 6)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
        var allCommitments = (await _profileRepository.GetFixedCommitmentsAsync(onlyActive: true)).ToList();

        var cycles = new List<string>();
        for (int i = 0; i < months; i++)
        {
            cycles.Add(BillingCycleCalculator.AddMonthsToCycle(currentActiveCycle, i));
        }

        var allExpensesInCycles = (await _expenseRepository.GetExpensesInCyclesAsync(cycles)).ToList();
        var projections = new List<ProjectionCycleDto>();

        // Recurring card subscriptions amount per month
        var monthlyCardSubscriptions = allCommitments
            .Where(c => string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Amount);

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

            var cardInstallments = allExpensesInCycles
                .Where(e => e.BillingCycle == cycle && e.PaymentMethod == "CARD")
                .Sum(e => e.Amount) + monthlyCardSubscriptions;

            var baseCap = (profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0)
                ? profile.CustomCardCap.Value
                : (profile.TotalIncome - profile.MonthlyInvestmentGoal - fixedCommitmentsTotal);

            var projectedFreeCap = baseCap - cardInstallments;
            var projectedDailyBurnRate = Math.Round(projectedFreeCap / 30m, 2);

            projections.Add(new ProjectionCycleDto
            {
                BillingCycle = cycle,
                FixedCommitmentsTotal = fixedCommitmentsTotal,
                CommittedCardInstallments = cardInstallments,
                ProjectedFreeCap = projectedFreeCap,
                ProjectedDailyBurnRate = projectedDailyBurnRate
            });
        }

        return projections;
    }

    public async Task<SimulatorResponseDto> SimulatePurchaseAsync(SimulatorRequestDto dto)
    {
        var installments = Math.Max(1, dto.Installments);
        var installmentValue = Math.Round(dto.Amount / installments, 2);

        var dashboard = await GetDashboardSummaryAsync();
        var currentDailyBefore = dashboard.AvailablePerDay;
        var remainingBudgetBefore = dashboard.RemainingBudget;
        var remainingBudgetAfter = remainingBudgetBefore - installmentValue;

        var willInvade = remainingBudgetAfter < 0;
        var invadedAmount = willInvade ? Math.Abs(remainingBudgetAfter) : 0m;

        var currentDailyAfter = (dashboard.DaysUntilClosing > 0 && remainingBudgetAfter > 0)
            ? Math.Round(remainingBudgetAfter / dashboard.DaysUntilClosing, 2)
            : 0m;

        var projections = await GetProjectionsAsync(Math.Max(6, installments));
        var affectedCycles = new List<SimulatorCycleImpactDto>();

        for (int i = 0; i < installments; i++)
        {
            var cycle = BillingCycleCalculator.AddMonthsToCycle(dashboard.BillingCycle, i);
            var proj = projections.FirstOrDefault(p => p.BillingCycle == cycle);
            var freeCapBefore = proj?.ProjectedFreeCap ?? 0m;
            var freeCapAfter = freeCapBefore - installmentValue;

            affectedCycles.Add(new SimulatorCycleImpactDto
            {
                BillingCycle = cycle,
                ProjectedFreeCapBefore = freeCapBefore,
                ProjectedFreeCapAfter = freeCapAfter,
                InstallmentAmount = installmentValue,
                WillInvade = freeCapAfter < 0
            });
        }

        return new SimulatorResponseDto
        {
            Amount = dto.Amount,
            Installments = installments,
            InstallmentValue = installmentValue,
            CurrentDailyBefore = currentDailyBefore,
            CurrentDailyAfter = currentDailyAfter,
            RemainingBudgetBefore = remainingBudgetBefore,
            RemainingBudgetAfter = remainingBudgetAfter,
            WillInvadeInvestmentGoal = willInvade,
            InvadedAmount = invadedAmount,
            AffectedCycles = affectedCycles
        };
    }
}
