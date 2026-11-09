using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.System;

public interface ISystemService
{
    Task<Result<SystemStatusModel>> GetStatusAsync();
}
