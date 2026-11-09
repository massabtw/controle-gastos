using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface IInvoicePaymentRepository
{
    Task<InvoicePaymentEntity?> GetByCycleAsync(string cycle);
    Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> TogglePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary);
    Task<(bool success, decimal surplusInvestedAmount, decimal newCaixinhaBalance)> InvestSurplusAsync(string cycle);
}
