using Dapper;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Domain.Services;
using FinanceTracker.Infrastructure.UnitOfWorks;

namespace FinanceTracker.Infrastructure.Repositories;

public class CardExpenseRepository : ICardExpenseRepository
{
    private readonly UnitOfWorkApp _unitOfWork;

    public CardExpenseRepository(UnitOfWorkApp unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CardExpenseEntity?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM CardExpenses WHERE Id = @id AND Deleted = 0";
        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, new { id }, _unitOfWork.Transaction);
    }

    public async Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid)
    {
        const string sql = "SELECT * FROM CardExpenses WHERE Uuid = @uuid AND Deleted = 0";
        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, new { uuid }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle)
    {
        const string sql = @"
            SELECT * FROM CardExpenses 
            WHERE BillingCycle = @cycle AND Deleted = 0
            ORDER BY TransactionDate DESC, Id DESC";

        return await _unitOfWork.Connection.QueryAsync<CardExpenseEntity>(sql, new { cycle }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10)
    {
        const string sql = @"
            SELECT TOP (@count) * FROM CardExpenses 
            WHERE Deleted = 0
            ORDER BY TransactionDate DESC, Id DESC";

        return await _unitOfWork.Connection.QueryAsync<CardExpenseEntity>(sql, new { count }, _unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(CardExpenseEntity entity)
    {
        const string sql = @"
            INSERT INTO CardExpenses (
                Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
            ) VALUES (
                @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                @DeductFromCycleCap, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
            );
            SELECT CAST(SCOPE_IDENTITY() as int);";

        if (entity.Uuid == Guid.Empty)
            entity.Uuid = Guid.NewGuid();

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(sql, entity, _unitOfWork.Transaction);
    }

    public async Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities)
    {
        const string sql = @"
            INSERT INTO CardExpenses (
                Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
            ) VALUES (
                @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                @DeductFromCycleCap, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
            );";

        foreach (var entity in entities)
        {
            if (entity.Uuid == Guid.Empty)
                entity.Uuid = Guid.NewGuid();
        }

        await _unitOfWork.Connection.ExecuteAsync(sql, entities, _unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
            UPDATE CardExpenses 
            SET Deleted = 1, UpdatedAt = SYSDATETIMEOFFSET() 
            WHERE Id = @id AND Deleted = 0";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { id }, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<CardExpenseEntity?> GetLastExpenseAsync()
    {
        const string sql = @"
            SELECT TOP 1 * FROM CardExpenses 
            WHERE Deleted = 0
            ORDER BY Id DESC";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, transaction: _unitOfWork.Transaction);
    }

    public async Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId)
    {
        const string sql = @"
            UPDATE CardExpenses 
            SET Deleted = 1, UpdatedAt = SYSDATETIMEOFFSET() 
            WHERE PurchaseGroupId = @purchaseGroupId AND Deleted = 0";

        return await _unitOfWork.Connection.ExecuteAsync(sql, new { purchaseGroupId }, _unitOfWork.Transaction);
    }

    public async Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle)
    {
        const string sql = @"
            SELECT ISNULL(SUM(Amount), 0.00) 
            FROM CardExpenses 
            WHERE BillingCycle > @targetCycle 
              AND PaymentMethod = 'CARD'
              AND Deleted = 0";

        return await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { targetCycle }, _unitOfWork.Transaction);
    }

    public async Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead)
    {
        var endCycle = BillingCycleCalculator.AddMonthsToCycle(startCycle, monthsAhead);

        const string sql = @"
            SELECT BillingCycle, ISNULL(SUM(Amount), 0.00) as Total
            FROM CardExpenses
            WHERE BillingCycle >= @startCycle AND BillingCycle <= @endCycle
              AND PaymentMethod = 'CARD'
              AND Deleted = 0
            GROUP BY BillingCycle";

        var results = await _unitOfWork.Connection.QueryAsync<(string BillingCycle, decimal Total)>(
            sql,
            new { startCycle, endCycle },
            _unitOfWork.Transaction);

        return results.ToDictionary(r => r.BillingCycle, r => r.Total);
    }
}
