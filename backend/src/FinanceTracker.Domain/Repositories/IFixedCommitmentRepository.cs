using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface IFixedCommitmentRepository
{
    Task<FixedCommitmentEntity?> GetByIdAsync(int id);
    Task<IEnumerable<FixedCommitmentEntity>> GetAllAsync(bool onlyActive = true);
    Task<int> CreateAsync(FixedCommitmentEntity entity);
    Task<bool> UpdateAsync(FixedCommitmentEntity entity);
    Task<bool> DeleteAsync(int id);
    Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle);
}
