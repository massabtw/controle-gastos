using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface ICardExpenseRepository
{
    Task<CardExpenseEntity?> GetByIdAsync(int id);
    Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid);
    Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle);
    Task<IEnumerable<CardExpenseEntity>> GetByGroupIdAsync(Guid purchaseGroupId);
    Task<IEnumerable<CardExpenseEntity>> GetExpensesInCyclesAsync(IEnumerable<string> cycles);
    Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10);
    Task<int> CreateAsync(CardExpenseEntity entity);
    Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities);
    Task<bool> UpdateAsync(CardExpenseEntity entity);
    Task<bool> UpdateGroupDetailsAsync(Guid purchaseGroupId, string description, string category);
    Task<bool> DeleteAsync(int id);
    Task<CardExpenseEntity?> GetLastExpenseAsync();
    Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId);
    Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle);
    Task<decimal> GetTotalCommittedCardExpensesFromCycleAsync(string fromCycle);
    Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead);
}
