using FinanceTracker.Api.Domain.Entities;

namespace FinanceTracker.Api.Infrastructure.Repositories;

public interface IExpenseRepository
{
    Task<IEnumerable<CardExpense>> GetExpensesByCycleAsync(string cycle);
    Task<CardExpense?> GetExpenseByIdAsync(int id);
    Task<IEnumerable<CardExpense>> GetExpensesByGroupIdAsync(Guid purchaseGroupId);
    Task<int> AddExpenseAsync(CardExpense expense);
    Task AddExpensesBatchAsync(IEnumerable<CardExpense> expenses);
    Task<bool> UpdateExpenseAsync(CardExpense expense);
    Task<bool> UpdateExpenseGroupDetailsAsync(Guid purchaseGroupId, string description, string category);
    Task<bool> DeleteExpenseAsync(int id);
    Task<int> DeleteExpenseGroupAsync(Guid purchaseGroupId);
    Task<CardExpense?> GetLatestExpenseAsync();
    Task<decimal> GetTotalByCycleAsync(string cycle);
    Task<IEnumerable<CardExpense>> GetExpensesInCyclesAsync(IEnumerable<string> cycles);
}
