using FinanceTracker.Api.Domain.Entities;

namespace FinanceTracker.Api.Infrastructure.Repositories;

public interface IProfileRepository
{
    Task<FinancialProfile> GetProfileAsync();
    Task<bool> UpdateProfileAsync(FinancialProfile profile);
    Task<bool> UpdateCaixinhaBalanceAsync(decimal balance);
    Task<decimal> DepositCaixinhaAsync(decimal amount);
    Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap);
    Task<bool> MarkCommitmentPaidFromCaixinhaAsync(int id, string cycle);
    Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle);
    Task<IEnumerable<FixedCommitment>> GetFixedCommitmentsAsync(bool onlyActive = true);
    Task<FixedCommitment?> GetFixedCommitmentByIdAsync(int id);
    Task<int> AddFixedCommitmentAsync(FixedCommitment commitment);
    Task<bool> UpdateFixedCommitmentAsync(FixedCommitment commitment);
    Task<bool> DeleteFixedCommitmentAsync(int id);
    Task<InvoicePayment?> GetInvoicePaymentAsync(string cycle);
    Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> ToggleInvoicePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary);
    Task<(bool success, decimal surplusInvestedAmount, decimal newCaixinhaBalance)> InvestInvoiceSurplusAsync(string cycle);
}
