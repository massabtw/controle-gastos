using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Profile;

public interface IFinancialProfileService
{
    Task<Result<FinancialProfileModel>> GetProfileAsync();
    Task<Result<FinancialProfileModel>> UpdateProfileAsync(UpdateFinancialProfileModel model);
    Task<Result<bool>> UpdateCaixinhaBalanceAsync(decimal balance);
    Task<Result<decimal>> DepositCaixinhaAsync(decimal amount);
    Task<Result<bool>> UpdateCardLimitAsync(UpdateCreditCardLimitModel model);
}
