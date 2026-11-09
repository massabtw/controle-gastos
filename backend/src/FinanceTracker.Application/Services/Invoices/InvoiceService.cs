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

    public async Task<Result<InvoiceTogglePaidResultModel>> TogglePaymentAsync(string? cycle)
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();
            var targetCycle = string.IsNullOrWhiteSpace(cycle)
                ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
                : cycle;

            var expenses = await _unitOfWork.CardExpense.GetByCycleAsync(targetCycle);
            var allCommitments = await _unitOfWork.FixedCommitment.GetAllAsync(onlyActive: true);
            var cardSubscriptionsTotal = allCommitments
                .Where(c => string.Equals(c.CommitmentType, "SUBSCRIPTION", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(c.PaymentSource, "CARD", StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrWhiteSpace(c.StartCycle) || string.CompareOrdinal(targetCycle, c.StartCycle) >= 0))
                .Sum(c => c.Amount);

            var currentInvoiceTotal = expenses.Where(e => e.PaymentMethod == "CARD").Sum(e => e.Amount) + cardSubscriptionsTotal;

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
