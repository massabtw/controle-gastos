using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Repositories;

public interface IFinancialProfileRepository
{
    Task<FinancialProfileEntity> GetProfileAsync();
    Task<bool> UpdateProfileAsync(FinancialProfileEntity profile);
    Task<bool> UpdateCaixinhaBalanceAsync(decimal balance);
    Task<decimal> DepositCaixinhaAsync(decimal amount);
    Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap);
}
