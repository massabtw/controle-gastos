using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Commitments;

public interface IFixedCommitmentService
{
    Task<Result<IEnumerable<FixedCommitmentModel>>> GetAllAsync(bool onlyActive = true);
    Task<Result<IEnumerable<FixedCommitmentModel>>> GetSubscriptionsAsync();
    Task<Result<FixedCommitmentModel>> GetByIdAsync(int id);
    Task<Result<FixedCommitmentModel>> CreateAsync(CreateFixedCommitmentModel model);
    Task<Result<FixedCommitmentModel>> CreateSubscriptionAsync(CreateFixedCommitmentModel model);
    Task<Result<MessageResponseModel>> UpdateAsync(int id, CreateFixedCommitmentModel model);
    Task<Result<MessageResponseModel>> DeleteAsync(int id);
    Task<Result<TogglePaidResultModel>> TogglePaidAsync(int id, string? cycle);
}
