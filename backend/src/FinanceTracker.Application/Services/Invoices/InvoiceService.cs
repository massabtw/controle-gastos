using FinanceTracker.Application.Interfaces.Invoices;
using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.Services;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IUnitOfWorkApp unitOfWork,
        ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<InvoicePaymentModel?>> GetInvoicePaymentAsync(string? cycle)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = string.IsNullOrWhiteSpace(cycle)
                ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
                : cycle;

            var entity = await _unitOfWork.InvoicePayment.GetByCycleAsync(targetCycle);
            return entity != null ? entity.ToModel() : Result<InvoicePaymentModel?>.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice payment for cycle {Cycle}", cycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<InvoiceTogglePaidResultModel>> TogglePaymentAsync(string? cycle, DateTime? referenceDate = null)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var now = referenceDate ?? BillingCycleCalculator.GetCurrentBrazilDate();
            var currentActiveCycle = BillingCycleCalculator.GetTargetBillingCycle(now, profile.ClosingDay);
            var targetCycle = string.IsNullOrWhiteSpace(cycle)
                ? currentActiveCycle
                : cycle;

            var expenses = (await _unitOfWork.CardExpense.GetByCycleAsync(targetCycle)).ToList();
            var allCommitments = await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true);

            var cycleComparison = string.CompareOrdinal(targetCycle, currentActiveCycle);
            var cycleState = cycleComparison < 0 ? "CLOSED" : (cycleComparison > 0 ? "FUTURE" : "CURRENT");

            decimal chargedSubscriptionsTotal = 0m;
            foreach (var commitment in allCommitments.Where(c =>
                string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(commitment.StartCycle) &&
                    string.CompareOrdinal(targetCycle, commitment.StartCycle) < 0)
                {
                    continue;
                }

                var billingDay = commitment.BillingDay ?? 1;
                if (billingDay < 1) billingDay = 1;
                var expectedChargeDate = BillingCycleCalculator.GetSubscriptionChargeDateForCycle(targetCycle, billingDay, profile.ClosingDay);

                bool isCharged = cycleState == "CLOSED" || (cycleState != "FUTURE" && now.Date >= expectedChargeDate.Date);

                var isMaterialized = expenses.Any(e =>
                    e.PaymentMethod == "CARD" &&
                    !string.IsNullOrWhiteSpace(e.Description) &&
                    !string.IsNullOrWhiteSpace(commitment.Title) &&
                    (string.Equals(e.Description.Trim(), commitment.Title.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     e.Description.Contains(commitment.Title, StringComparison.OrdinalIgnoreCase) ||
                     commitment.Title.Contains(e.Description, StringComparison.OrdinalIgnoreCase)) &&
                    Math.Abs(e.Amount - commitment.Amount) <= 1.00m);

                if (!isMaterialized && isCharged)
                {
                    chargedSubscriptionsTotal += commitment.Amount;
                }
            }

            var currentInvoiceTotal = expenses.Where(e => e.PaymentMethod == "CARD").Sum(e => e.Amount) + chargedSubscriptionsTotal;

            var (isPaid, paidAmount, surplusAmount, surplusInvested, paidAt) =
                await _unitOfWork.InvoicePayment.TogglePaymentAsync(targetCycle, currentInvoiceTotal, profile.IncomeSecondTranche);

            return new InvoiceTogglePaidResultModel
            {
                Cycle = targetCycle,
                IsPaid = isPaid,
                PaidAmount = paidAmount,
                SurplusAmount = surplusAmount,
                SurplusInvested = surplusInvested,
                PaidAt = paidAt,
                Message = isPaid
                    ? $"Fatura do ciclo {targetCycle} marcada como PAGA! (Valor: R$ {paidAmount:F2}, Sobra do Salário: R$ {surplusAmount:F2})"
                    : $"Fatura do ciclo {targetCycle} desmarcada. Status retornado para pendente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling invoice payment for cycle {Cycle}", cycle);
            return ErrorMessages.UnknownError;
        }
    }

    public async Task<Result<InvestSurplusResultModel>> InvestSurplusAsync(string? cycle)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = string.IsNullOrWhiteSpace(cycle)
                ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
                : cycle;

            var (success, surplusInvestedAmount, newCaixinhaBalance) =
                await _unitOfWork.InvoicePayment.InvestSurplusAsync(targetCycle);

            if (!success)
            {
                return Result<InvestSurplusResultModel>.Failure("Fatura ainda não está marcada como paga ou não há sobra para investir.", 400);
            }

            return new InvestSurplusResultModel
            {
                Cycle = targetCycle,
                InvestedAmount = surplusInvestedAmount,
                SurplusInvestedAmount = surplusInvestedAmount,
                NewCaixinhaBalance = newCaixinhaBalance,
                Message = $"Sobra de R$ {surplusInvestedAmount:F2} transferida e investida com sucesso na Caixinha! Novo saldo: R$ {newCaixinhaBalance:F2}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error investing invoice surplus for cycle {Cycle}", cycle);
            return ErrorMessages.UnknownError;
        }
    }
}
