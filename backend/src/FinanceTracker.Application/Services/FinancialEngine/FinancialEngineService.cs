using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;
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

            var activeBillingCycle = BillingCycleCalculator.GetTargetBillingCycle(now, profile.ClosingDay);
            var inspectedCycle = targetCycle ?? activeBillingCycle;

            var (closingDate, dueDate) = BillingCycleCalculator.GetCycleDates(inspectedCycle, profile.ClosingDay, profile.DueDay);
            var (cycleStartDate, cycleEndDate) = BillingCycleCalculator.GetCyclePeriod(inspectedCycle, profile.ClosingDay);

            var daysUntilClosing = BillingCycleCalculator.GetDaysUntilClosing(now, profile.ClosingDay, inspectedCycle);

            var cycleExpensesEntities = (await _unitOfWork.CardExpense.GetByCycleAsync(inspectedCycle)).ToList();

            var currentInvoiceTotal = cycleExpensesEntities
                .Where(e => e.PaymentMethod == "CARD")
                .Sum(e => e.Amount);

            var pixCaixinhaUsedInCycle = cycleExpensesEntities
                .Where(e => e.PaymentMethod == "PIX_CAIXINHA" && e.DeductFromCycleCap)
                .Sum(e => e.Amount);

            var totalSpentInCycle = currentInvoiceTotal + pixCaixinhaUsedInCycle;

            var allCommitments = (await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true)).ToList();
            var activeCommitmentsForCycle = new List<ActiveCommitmentModel>();
            decimal fixedCommitmentsTotal = 0.00m;

            foreach (var commitment in allCommitments)
            {
                var status = BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, inspectedCycle);
                if (status != null)
                {
                    activeCommitmentsForCycle.Add(commitment.ToActiveModel(status, inspectedCycle));

                    var countsTowardsCap = commitment.CommitmentType != "SUBSCRIPTION" ||
                        string.IsNullOrWhiteSpace(commitment.PaymentSource) ||
                        commitment.PaymentSource == "CARD" ||
                        commitment.PaymentSource == "PIX_CAIXINHA";

                    if (countsTowardsCap)
                    {
                        fixedCommitmentsTotal += commitment.Amount;
                    }
                }
            }

            var monthlySpendingCap = profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0
                ? profile.CustomCardCap.Value
                : Math.Max(0, profile.TotalIncome - profile.MonthlyInvestmentGoal - fixedCommitmentsTotal);

            var remainingBudget = monthlySpendingCap - totalSpentInCycle;
            var availablePerDay = daysUntilClosing > 0 ? remainingBudget / daysUntilClosing : 0;

            var totalCycleDays = Math.Max(1, (cycleEndDate.Date - cycleStartDate.Date).Days);
            var daysElapsed = Math.Clamp((now.Date - cycleStartDate.Date).Days, 0, totalCycleDays);
            var idealBurnPacePercent = (decimal)daysElapsed / totalCycleDays * 100.0m;
            var burnPacePercent = monthlySpendingCap > 0 ? (totalSpentInCycle / monthlySpendingCap) * 100.0m : 0;

            var totalInstallmentsOnCard = await _unitOfWork.CardExpense.GetFutureCommittedInstallmentsTotalAsync(inspectedCycle);
            var bankLimitAvailable = Math.Max(0, profile.CreditCardLimit - (currentInvoiceTotal + totalInstallmentsOnCard));

            var effectiveAnnualRate = (profile.CdiAnnualRate * (profile.NubankCaixinhaCdiRate / 100.0m)) / 100.0m;
            var monthlyRate = (decimal)(Math.Pow((double)(1.0m + effectiveAnnualRate), 1.0 / 12.0) - 1.0);
            var caixinhaMonthlyYield = profile.NubankCaixinhaBalance * monthlyRate;
            var caixinhaDailyYield = caixinhaMonthlyYield / 30.0m;
            var projectedBalanceWithGoal = profile.NubankCaixinhaBalance + profile.MonthlyInvestmentGoal + caixinhaMonthlyYield;

            var invoicePayment = await _unitOfWork.InvoicePayment.GetByCycleAsync(inspectedCycle);
            var isInvoicePaid = invoicePayment != null && invoicePayment.IsPaid;
            var invoicePaidAmount = invoicePayment?.PaidAmount ?? 0m;
            var invoiceSurplusAmount = isInvoicePaid ? (invoicePayment?.SurplusAmount ?? Math.Max(0, profile.IncomeSecondTranche - invoicePaidAmount)) : 0m;
            var isSurplusInvested = invoicePayment != null && invoicePayment.SurplusInvested;
            var invoicePaidAt = invoicePayment?.PaidAt;

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

            var futureInstallmentsByCycle = await _unitOfWork.CardExpense.GetFutureCommittedInstallmentsByCycleAsync(inspectedCycle, 6);
            var futureProjections = new List<FutureCycleProjectionModel>();

            for (var m = 1; m <= 6; m++)
            {
                var cycleKey = BillingCycleCalculator.AddMonthsToCycle(inspectedCycle, m);
                futureInstallmentsByCycle.TryGetValue(cycleKey, out var commInstallments);

                decimal futureFixed = 0.00m;
                foreach (var c in allCommitments)
                {
                    if (BillingCycleCalculator.GetCommitmentInstallmentStatus(c, cycleKey) != null)
                    {
                        var countsTowardsCap = c.CommitmentType != "SUBSCRIPTION" ||
                            string.IsNullOrWhiteSpace(c.PaymentSource) ||
                            c.PaymentSource == "CARD" ||
                            c.PaymentSource == "PIX_CAIXINHA";

                        if (countsTowardsCap)
                        {
                            futureFixed += c.Amount;
                        }
                    }
                }

                var futureSpendingCap = profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0
                    ? profile.CustomCardCap.Value
                    : Math.Max(0, profile.TotalIncome - profile.MonthlyInvestmentGoal - futureFixed);

                var totalExpected = commInstallments;
                var remaining = futureSpendingCap - totalExpected;

                futureProjections.Add(new FutureCycleProjectionModel
                {
                    Cycle = cycleKey,
                    CommittedInstallments = commInstallments,
                    FixedCommitments = futureFixed,
                    TotalExpected = totalExpected,
                    SafeSpendingCap = futureSpendingCap,
                    RemainingAvailable = remaining
                });
            }

            var recentEntities = (await _unitOfWork.CardExpense.GetRecentAsync(15)).ToList();

            var summary = new DashboardSummaryModel
            {
                CurrentBillingCycle = inspectedCycle,
                CycleClosingDate = closingDate,
                CycleDueDate = dueDate,
                DaysUntilClosing = daysUntilClosing,

                TotalIncome = profile.TotalIncome,
                MonthlyInvestmentGoal = profile.MonthlyInvestmentGoal,
                FixedCommitmentsTotal = fixedCommitmentsTotal,
                MonthlySpendingCap = monthlySpendingCap,

                CreditCardBankLimit = profile.CreditCardLimit,
                TotalInstallmentsOnCard = totalInstallmentsOnCard,
                BankLimitAvailable = bankLimitAvailable,
                CustomCardCapOverride = profile.CustomCardCap,

                TotalSpentInCycle = totalSpentInCycle,
                CurrentInvoiceTotal = currentInvoiceTotal,
                PixCaixinhaUsedInCycle = pixCaixinhaUsedInCycle,
                RemainingBudget = remainingBudget,
                AvailablePerDay = availablePerDay,
                BurnPacePercent = burnPacePercent,
                IdealBurnPacePercent = idealBurnPacePercent,

                CaixinhaInvestedBalance = profile.NubankCaixinhaBalance,
                CaixinhaMonthlyYield = caixinhaMonthlyYield,
                CaixinhaDailyYield = caixinhaDailyYield,
                ProjectedBalanceWithGoal = projectedBalanceWithGoal,

                IsInvoicePaid = isInvoicePaid,
                InvoicePaidAmount = invoicePaidAmount,
                InvoiceSurplusAmount = invoiceSurplusAmount,
                IsSurplusInvested = isSurplusInvested,
                InvoicePaidAt = invoicePaidAt,

                RecentExpenses = recentEntities.ToModel().ToList(),
                ActiveCommitments = activeCommitmentsForCycle,
                DailyBurnHistory = dailyBurnHistory,
                FutureProjections = futureProjections
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard summary for targetCycle: {TargetCycle}", targetCycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<SimulatorResultModel>> SimulatePurchaseAsync(SimulatorRequestModel request)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = request.TargetCycle ?? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);

            var installments = Math.Max(1, request.Installments);
            var installmentValue = Math.Round(request.Amount / installments, 2);

            var allCommitments = (await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true)).ToList();
            var futureInstallmentsByCycle = await _unitOfWork.CardExpense.GetFutureCommittedInstallmentsByCycleAsync(targetCycle, installments + 2);

            var monthlyImpacts = new List<SimulatorMonthImpactModel>();
            var allMonthsViable = true;

            for (var i = 0; i < installments; i++)
            {
                var cycle = BillingCycleCalculator.AddMonthsToCycle(targetCycle, i);

                decimal fixedTotal = 0.00m;
                foreach (var c in allCommitments)
                {
                    if (BillingCycleCalculator.GetCommitmentInstallmentStatus(c, cycle) != null)
                    {
                        var countsTowardsCap = c.CommitmentType != "SUBSCRIPTION" ||
                            string.IsNullOrWhiteSpace(c.PaymentSource) ||
                            c.PaymentSource == "CARD" ||
                            c.PaymentSource == "PIX_CAIXINHA";

                        if (countsTowardsCap)
                        {
                            fixedTotal += c.Amount;
                        }
                    }
                }

                var cap = profile.CustomCardCap.HasValue && profile.CustomCardCap.Value > 0
                    ? profile.CustomCardCap.Value
                    : Math.Max(0, profile.TotalIncome - profile.MonthlyInvestmentGoal - fixedTotal);

                futureInstallmentsByCycle.TryGetValue(cycle, out var committedInst);

                var totalCommitted = committedInst + installmentValue;
                var remaining = cap - totalCommitted;
                var exceeds = remaining < 0;

                if (exceeds)
                {
                    allMonthsViable = false;
                }

                monthlyImpacts.Add(new SimulatorMonthImpactModel
                {
                    Cycle = cycle,
                    MonthlySpendingCap = cap,
                    AlreadyCommittedInstallments = committedInst,
                    FixedCommitments = fixedTotal,
                    SimulatedInstallment = installmentValue,
                    TotalExpectedCommitment = totalCommitted,
                    RemainingAvailableCap = remaining,
                    ExceedsCap = exceeds
                });
            }

            var effectiveAnnualRate = (profile.CdiAnnualRate * (profile.NubankCaixinhaCdiRate / 100.0m)) / 100.0m;
            var monthlyRate = (decimal)(Math.Pow((double)(1.0m + effectiveAnnualRate), 1.0 / 12.0) - 1.0);
            var caixinhaMonthlyYield = profile.NubankCaixinhaBalance * monthlyRate;

            string recommendation;
            if (allMonthsViable)
            {
                recommendation = $"Compra viável! A parcela de R$ {installmentValue:F2} cabe no teto de todos os {installments} meses simulados sem comprometer a meta de investimento de R$ {profile.MonthlyInvestmentGoal:F2}.";
            }
            else
            {
                recommendation = $"Atenção: A parcela de R$ {installmentValue:F2} estoura o teto de gastos seguro em um ou mais meses futuros. Considere parcelar em mais vezes ou adiar a compra para não comprometer a meta de investimento.";
            }

            return new SimulatorResultModel
            {
                TotalAmount = request.Amount,
                Installments = installments,
                InstallmentValue = installmentValue,
                IsViable = allMonthsViable,
                Recommendation = recommendation,
                CurrentCaixinhaYield = caixinhaMonthlyYield,
                MonthlyImpacts = monthlyImpacts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating purchase for amount {Amount}", request.Amount);
            return ErrorMessages.UnknownError;
        }
    }
}
