using FinanceTracker.Api.Domain.Entities;

namespace FinanceTracker.Api.Infrastructure.Repositories;

public interface IProfileRepository
{
    Task<FinancialProfile> GetProfileAsync();
    Task<bool> UpdateProfileAsync(FinancialProfile profile);
    Task<bool> UpdateCaixinhaBalanceAsync(decimal balance);
    Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap);
    Task<bool> MarkCommitmentPaidFromCaixinhaAsync(int id, string cycle);
    Task<IEnumerable<FixedCommitment>> GetFixedCommitmentsAsync(bool onlyActive = true);
    Task<FixedCommitment?> GetFixedCommitmentByIdAsync(int id);
    Task<int> AddFixedCommitmentAsync(FixedCommitment commitment);
    Task<bool> UpdateFixedCommitmentAsync(FixedCommitment commitment);
    Task<bool> DeleteFixedCommitmentAsync(int id);
}
