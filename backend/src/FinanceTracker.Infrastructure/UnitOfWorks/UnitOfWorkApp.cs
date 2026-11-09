using FinanceTracker.Domain.Repositories;
using FinanceTracker.Domain.UnitOfWorks;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.UnitOfWorks;

public class UnitOfWorkApp : UnitOfWork, IUnitOfWorkApp
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
        var rawConnStr = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("FinanceTrackerDb")
            ?? configuration.GetConnectionString("AppDb")
            ?? string.Empty;

        var (resolvedConn, isPg) = ConnectionStringParser.Resolve(rawConnStr);
        ConnectionString = resolvedConn;
        IsPostgres = isPg;
    }
}
