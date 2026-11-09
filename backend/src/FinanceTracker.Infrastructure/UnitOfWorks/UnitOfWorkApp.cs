using FinanceTracker.Domain.Repositories;
using FinanceTracker.Domain.UnitOfWorks;
using FinanceTracker.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.UnitOfWorks;

public class UnitOfWorkApp : UnitOfWork<SqlConnection>, IUnitOfWorkApp
{
    private ICardExpenseRepository? _cardExpense;
    private IFinancialProfileRepository? _financialProfile;
    private IFixedCommitmentRepository? _fixedCommitment;
    private IInvoicePaymentRepository? _invoicePayment;

    public ICardExpenseRepository CardExpense => _cardExpense ??= new CardExpenseRepository(this);
    public IFinancialProfileRepository FinancialProfile => _financialProfile ??= new FinancialProfileRepository(this);
    public IFixedCommitmentRepository FixedCommitment => _fixedCommitment ??= new FixedCommitmentRepository(this);
    public IInvoicePaymentRepository InvoicePayment => _invoicePayment ??= new InvoicePaymentRepository(this);

    public UnitOfWorkApp(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("FinanceTrackerDb")
            ?? configuration.GetConnectionString("AppDb");
    }
}
