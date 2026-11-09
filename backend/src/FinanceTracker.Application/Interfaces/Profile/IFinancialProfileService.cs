using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Profile;

public interface IFinancialProfileService
{
    Task<Result<FinancialProfileModel>> GetProfileAsync();
    Task<Result<ProfileWithCommitmentsModel>> GetProfileWithCommitmentsAsync();
    Task<Result<UpdateProfileResultModel>> UpdateProfileAsync(UpdateFinancialProfileModel model);
    Task<Result<CaixinhaOperationResultModel>> UpdateCaixinhaBalanceAsync(decimal balance);
    Task<Result<CaixinhaOperationResultModel>> DepositCaixinhaAsync(decimal amount);
    Task<Result<UpdateCardLimitResultModel>> UpdateCardLimitAsync(UpdateCreditCardLimitModel model);
}
