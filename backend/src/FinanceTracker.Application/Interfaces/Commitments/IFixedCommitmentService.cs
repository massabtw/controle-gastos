using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Commitments;

public interface IFixedCommitmentService
{
    Task<Result<IEnumerable<FixedCommitmentModel>>> GetAllAsync(bool onlyActive = true);
    Task<Result<FixedCommitmentModel>> GetByIdAsync(int id);
    Task<Result<FixedCommitmentModel>> CreateAsync(CreateFixedCommitmentModel model);
    Task<Result<bool>> UpdateAsync(int id, CreateFixedCommitmentModel model);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<object>> TogglePaidAsync(int id, string? cycle);
}
