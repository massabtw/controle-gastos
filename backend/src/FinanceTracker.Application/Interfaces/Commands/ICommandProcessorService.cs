using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Commands;

public interface ICommandProcessorService
{
    Task<Result<CommandResponseModel>> ProcessCommandAsync(string input);
}
