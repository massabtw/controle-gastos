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

        // Calculate active commitments for this cycle
        var allCommitments = await _profileRepository.GetFixedCommitmentsAsync(onlyActive: true);
        var activeCommitmentsList = new List<ActiveCommitmentDto>();
        decimal totalFixedCommitments = 0m;

        foreach (var commitment in allCommitments)
        {
            var badge = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, targetCycle);
            if (badge != null)
            {
                activeCommitmentsList.Add(new ActiveCommitmentDto
                {
                    Id = commitment.Id,
                    Title = commitment.Title,
                    Amount = commitment.Amount,
                    Badge = badge
                });
                totalFixedCommitments += commitment.Amount;
            }
        }

        // Hard Cap Formula: TotalIncome - InvestmentGoal - TotalFixedCommitments
        var totalIncome = profile.TotalIncome;
        var cardHardCap = totalIncome - profile.MonthlyInvestmentGoal - totalFixedCommitments;

        // Card expenses in target cycle
        var expenses = (await _expenseRepository.GetExpensesByCycleAsync(targetCycle)).ToList();
        var currentInvoiceTotal = expenses.Sum(e => e.Amount);
        var remainingBudget = cardHardCap - currentInvoiceTotal;

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

        // Category breakdown
        var categoryGroups = expenses
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

            ActiveFixedCommitments = activeCommitmentsList,
            TotalFixedCommitments = totalFixedCommitments,

            CardHardCap = cardHardCap,
            CurrentInvoiceTotal = currentInvoiceTotal,
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

            CategoryBreakdown = categoryGroups
        };
    }

    public async Task<List<ProjectionCycleDto>> GetProjectionsAsync(int months = 6)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
        var allCommitments = await _profileRepository.GetFixedCommitmentsAsync(onlyActive: true);

        var cycles = new List<string>();
        for (int i = 0; i < months; i++)
        {
            cycles.Add(BillingCycleCalculator.AddMonthsToCycle(currentActiveCycle, i));
        }

        var allExpensesInCycles = (await _expenseRepository.GetExpensesInCyclesAsync(cycles)).ToList();
        var projections = new List<ProjectionCycleDto>();

        foreach (var cycle in cycles)
        {
            decimal fixedCommitmentsTotal = 0m;
            foreach (var commitment in allCommitments)
            {
                var badge = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, cycle);
                if (badge != null)
                {
                    fixedCommitmentsTotal += commitment.Amount;
                }
            }

            var cardInstallments = allExpensesInCycles
                .Where(e => e.BillingCycle == cycle)
                .Sum(e => e.Amount);

            var projectedFreeCap = profile.TotalIncome - profile.MonthlyInvestmentGoal - fixedCommitmentsTotal - cardInstallments;
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
