using FinanceTracker.Application.Interfaces.System;
using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;
using FinanceTracker.Domain.Services;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.System;

public class SystemService : ISystemService
{
    private readonly IUnitOfWorkApp _unitOfWork;
    private readonly ILogger<SystemService> _logger;

    public SystemService(
        IUnitOfWorkApp unitOfWork,
        ILogger<SystemService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SystemStatusModel>> GetStatusAsync()
    {
        try
        {
            var profile = await _unitOfWork.FinancialProfile.GetProfileAsync();

            var currentCycle = BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay);
            var (closingDate, dueDate) = BillingCycleCalculator.GetCycleDates(currentCycle, profile.ClosingDay, profile.DueDay);

            return new SystemStatusModel
            {
                Status = "ONLINE",
                DatabaseConnected = true,
                DatabaseError = null,
                ActiveBillingCycle = currentCycle,
                CycleClosingDate = closingDate,
                CycleDueDate = dueDate,
                ServerTime = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection error in SystemService");
            var dbError = ex.Message;

            return new SystemStatusModel
            {
                Status = "DEGRADED",
                DatabaseConnected = false,
                DatabaseError = dbError,
                ActiveBillingCycle = string.Empty,
                CycleClosingDate = DateTime.MinValue,
                CycleDueDate = DateTime.MinValue,
                ServerTime = DateTime.Now
            };
        }
    }
}
