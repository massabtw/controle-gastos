using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Expenses;

public interface IExpenseService
{
    Task<Result<IEnumerable<ExpenseItemModel>>> GetExpensesByCycleAsync(string cycle);
    Task<Result<ExpenseItemModel>> CreateExpenseAsync(CreateExpenseModel model);
    Task<Result<bool>> DeleteExpenseAsync(int id);
    Task<Result<bool>> DeleteLastExpenseAsync();
    Task<Result<ExpenseItemModel>> ReceivePixAsync(ReceivePixModel model);
}
