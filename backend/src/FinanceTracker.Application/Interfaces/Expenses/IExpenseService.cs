using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Expenses;

public interface IExpenseService
{
    Task<Result<IEnumerable<ExpenseItemModel>>> GetExpensesByCycleAsync(string? cycle = null);
    Task<Result<ExpenseItemModel>> GetExpenseByIdAsync(int id);
    Task<Result<IEnumerable<ExpenseItemModel>>> CreateExpenseAsync(CreateExpenseModel model);
    Task<Result<ExpenseItemModel>> CreatePixCaixinhaExpenseAsync(CreateExpenseModel model);
    Task<Result<ExpenseItemModel>> ReceivePixAsync(ReceivePixModel model);
    Task<Result<MessageResponseModel>> UpdateExpenseAsync(int id, UpdateExpenseModel model);
    Task<Result<MessageResponseModel>> DeleteExpenseAsync(int id, bool deleteAllInstallments = false);
    Task<Result<MessageResponseModel>> DeleteLastExpenseAsync();
    Task<Result<SyncInvoiceResultModel>> SyncInvoiceAsync(SyncInvoiceModel model);
}
