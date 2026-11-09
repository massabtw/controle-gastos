using FinanceTracker.Domain.Repositories;

namespace FinanceTracker.Domain.UnitOfWorks;

public interface IUnitOfWorkApp : IUnitOfWork
{
    ICardExpenseRepository CardExpense { get; }
    IFinancialProfileRepository FinancialProfile { get; }
    IFixedCommitmentRepository FixedCommitment { get; }
    IInvoicePaymentRepository InvoicePayment { get; }
}
