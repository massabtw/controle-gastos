using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.FinancialEngine;

public interface IFinancialEngineService
{
    Task<Result<DashboardSummaryModel>> GetDashboardSummaryAsync(string? targetCycle = null, DateTime? referenceDate = null);
    Task<Result<IEnumerable<FutureCycleProjectionModel>>> GetProjectionsAsync(int months = 6);
    Task<Result<SimulatorResultModel>> SimulatePurchaseAsync(SimulatorRequestModel request);
}
