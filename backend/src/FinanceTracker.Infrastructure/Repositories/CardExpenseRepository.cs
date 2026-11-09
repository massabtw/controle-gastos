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
        var sql = _unitOfWork.IsPostgres
            ? "SELECT * FROM CardExpenses WHERE Id = @id AND Deleted = FALSE"
            : "SELECT * FROM CardExpenses WHERE Id = @id AND Deleted = 0";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, new { id }, _unitOfWork.Transaction);
    }

    public async Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid)
    {
        var sql = _unitOfWork.IsPostgres
            ? "SELECT * FROM CardExpenses WHERE Uuid = @uuid AND Deleted = FALSE"
            : "SELECT * FROM CardExpenses WHERE Uuid = @uuid AND Deleted = 0";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, new { uuid }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT * FROM CardExpenses 
                WHERE BillingCycle = @cycle AND Deleted = FALSE
                ORDER BY TransactionDate DESC, Id DESC"
            : @"SELECT * FROM CardExpenses 
                WHERE BillingCycle = @cycle AND Deleted = 0
                ORDER BY TransactionDate DESC, Id DESC";

        return await _unitOfWork.Connection.QueryAsync<CardExpenseEntity>(sql, new { cycle }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT * FROM CardExpenses 
                WHERE Deleted = FALSE
                ORDER BY TransactionDate DESC, Id DESC
                LIMIT @count"
            : @"SELECT TOP (@count) * FROM CardExpenses 
                WHERE Deleted = 0
                ORDER BY TransactionDate DESC, Id DESC";

        return await _unitOfWork.Connection.QueryAsync<CardExpenseEntity>(sql, new { count }, _unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(CardExpenseEntity entity)
    {
        if (entity.Uuid == Guid.Empty)
            entity.Uuid = Guid.NewGuid();

        var sql = _unitOfWork.IsPostgres
            ? @"INSERT INTO CardExpenses (
                    Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                    InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                    DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                    @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                    @DeductFromCycleCap, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE
                ) RETURNING Id;"
            : @"INSERT INTO CardExpenses (
                    Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                    InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                    DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                    @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                    @DeductFromCycleCap, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(sql, entity, _unitOfWork.Transaction);
    }

    public async Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Uuid == Guid.Empty)
                entity.Uuid = Guid.NewGuid();
        }

        var sql = _unitOfWork.IsPostgres
            ? @"INSERT INTO CardExpenses (
                    Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                    InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                    DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                    @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                    @DeductFromCycleCap, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE
                );"
            : @"INSERT INTO CardExpenses (
                    Uuid, Description, Category, Amount, TransactionDate, BillingCycle, 
                    InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, 
                    DeductFromCycleCap, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Description, @Category, @Amount, @TransactionDate, @BillingCycle, 
                    @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, 
                    @DeductFromCycleCap, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
                );";

        await _unitOfWork.Connection.ExecuteAsync(sql, entities, _unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE CardExpenses 
                SET Deleted = TRUE, UpdatedAt = CURRENT_TIMESTAMP 
                WHERE Id = @id AND Deleted = FALSE"
            : @"UPDATE CardExpenses 
                SET Deleted = 1, UpdatedAt = SYSDATETIMEOFFSET() 
                WHERE Id = @id AND Deleted = 0";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { id }, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<CardExpenseEntity?> GetLastExpenseAsync()
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT * FROM CardExpenses 
                WHERE Deleted = FALSE
                ORDER BY Id DESC
                LIMIT 1"
            : @"SELECT TOP 1 * FROM CardExpenses 
                WHERE Deleted = 0
                ORDER BY Id DESC";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<CardExpenseEntity>(sql, transaction: _unitOfWork.Transaction);
    }

    public async Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE CardExpenses 
                SET Deleted = TRUE, UpdatedAt = CURRENT_TIMESTAMP 
                WHERE PurchaseGroupId = @purchaseGroupId AND Deleted = FALSE"
            : @"UPDATE CardExpenses 
                SET Deleted = 1, UpdatedAt = SYSDATETIMEOFFSET() 
                WHERE PurchaseGroupId = @purchaseGroupId AND Deleted = 0";

        return await _unitOfWork.Connection.ExecuteAsync(sql, new { purchaseGroupId }, _unitOfWork.Transaction);
    }

    public async Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT COALESCE(SUM(Amount), 0.00) 
                FROM CardExpenses 
                WHERE BillingCycle > @targetCycle 
                  AND PaymentMethod = 'CARD'
                  AND Deleted = FALSE"
            : @"SELECT ISNULL(SUM(Amount), 0.00) 
                FROM CardExpenses 
                WHERE BillingCycle > @targetCycle 
                  AND PaymentMethod = 'CARD'
                  AND Deleted = 0";

        return await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { targetCycle }, _unitOfWork.Transaction);
    }

    public async Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead)
    {
        var endCycle = BillingCycleCalculator.AddMonthsToCycle(startCycle, monthsAhead);

        var sql = _unitOfWork.IsPostgres
            ? @"SELECT BillingCycle, COALESCE(SUM(Amount), 0.00) as Total
                FROM CardExpenses
                WHERE BillingCycle >= @startCycle AND BillingCycle <= @endCycle
                  AND PaymentMethod = 'CARD'
                  AND Deleted = FALSE
                GROUP BY BillingCycle"
            : @"SELECT BillingCycle, ISNULL(SUM(Amount), 0.00) as Total
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
