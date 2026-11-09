using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.FinancialEngine;

public interface IFinancialEngineService
{
    Task<Result<DashboardSummaryModel>> GetDashboardSummaryAsync(string? targetCycle = null);
    Task<Result<SimulatorResultModel>> SimulatePurchaseAsync(SimulatorRequestModel request);
}
