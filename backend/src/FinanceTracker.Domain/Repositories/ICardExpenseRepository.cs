using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface ICardExpenseRepository
{
    Task<CardExpenseEntity?> GetByIdAsync(int id);
    Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid);
    Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle);
    Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10);
    Task<int> CreateAsync(CardExpenseEntity entity);
    Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities);
    Task<bool> DeleteAsync(int id);
    Task<CardExpenseEntity?> GetLastExpenseAsync();
    Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId);
    Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle);
    Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead);
}
