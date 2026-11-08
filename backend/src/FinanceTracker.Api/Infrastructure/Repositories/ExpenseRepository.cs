using Dapper;
using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Infrastructure.Data;

namespace FinanceTracker.Api.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExpenseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CardExpense>> GetExpensesByCycleAsync(string cycle)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap
            FROM dbo.CardExpenses
            WHERE BillingCycle = @BillingCycle
            ORDER BY TransactionDate DESC, Id DESC;";

        return await connection.QueryAsync<CardExpense>(sql, new { BillingCycle = cycle });
    }

    public async Task<CardExpense?> GetExpenseByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap
            FROM dbo.CardExpenses
            WHERE Id = @Id;";

        return await connection.QueryFirstOrDefaultAsync<CardExpense>(sql, new { Id = id });
    }

    public async Task<IEnumerable<CardExpense>> GetExpensesByGroupIdAsync(Guid purchaseGroupId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap
            FROM dbo.CardExpenses
            WHERE PurchaseGroupId = @PurchaseGroupId
            ORDER BY InstallmentCurrent ASC;";

        return await connection.QueryAsync<CardExpense>(sql, new { PurchaseGroupId = purchaseGroupId });
    }

    public async Task<int> AddExpenseAsync(CardExpense expense)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.CardExpenses 
                (Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap)
            OUTPUT INSERTED.Id
            VALUES 
                (@Description, @Category, @Amount, @TransactionDate, @BillingCycle, @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, @DeductFromCycleCap);";

        return await connection.ExecuteScalarAsync<int>(sql, expense);
    }

    public async Task AddExpensesBatchAsync(IEnumerable<CardExpense> expenses)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.CardExpenses 
                (Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap)
            VALUES 
                (@Description, @Category, @Amount, @TransactionDate, @BillingCycle, @InstallmentCurrent, @InstallmentTotal, @PurchaseGroupId, @PaymentMethod, @DeductFromCycleCap);";

        await connection.ExecuteAsync(sql, expenses);
    }

    public async Task<bool> UpdateExpenseAsync(CardExpense expense)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.CardExpenses
            SET Description = @Description,
                Category = @Category,
                Amount = @Amount,
                TransactionDate = @TransactionDate,
                BillingCycle = @BillingCycle,
                InstallmentCurrent = @InstallmentCurrent,
                InstallmentTotal = @InstallmentTotal,
                PurchaseGroupId = @PurchaseGroupId,
                PaymentMethod = @PaymentMethod,
                DeductFromCycleCap = @DeductFromCycleCap
            WHERE Id = @Id;";

        var affected = await connection.ExecuteAsync(sql, expense);
        return affected > 0;
    }

    public async Task<bool> UpdateExpenseGroupDetailsAsync(Guid purchaseGroupId, string description, string category)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.CardExpenses
            SET Description = @Description,
                Category = @Category
            WHERE PurchaseGroupId = @PurchaseGroupId;";

        var affected = await connection.ExecuteAsync(sql, new { PurchaseGroupId = purchaseGroupId, Description = description, Category = category });
        return affected > 0;
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"DELETE FROM dbo.CardExpenses WHERE Id = @Id;";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<int> DeleteExpenseGroupAsync(Guid purchaseGroupId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"DELETE FROM dbo.CardExpenses WHERE PurchaseGroupId = @PurchaseGroupId;";
        return await connection.ExecuteAsync(sql, new { PurchaseGroupId = purchaseGroupId });
    }

    public async Task<CardExpense?> GetLatestExpenseAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 Id, Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap
            FROM dbo.CardExpenses
            ORDER BY Id DESC;";

        return await connection.QueryFirstOrDefaultAsync<CardExpense>(sql);
    }

    public async Task<decimal> GetTotalByCycleAsync(string cycle)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ISNULL(SUM(Amount), 0)
            FROM dbo.CardExpenses
            WHERE BillingCycle = @BillingCycle AND PaymentMethod = 'CARD';";

        return await connection.ExecuteScalarAsync<decimal>(sql, new { BillingCycle = cycle });
    }

    public async Task<IEnumerable<CardExpense>> GetExpensesInCyclesAsync(IEnumerable<string> cycles)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Description, Category, Amount, TransactionDate, BillingCycle, InstallmentCurrent, InstallmentTotal, PurchaseGroupId, PaymentMethod, DeductFromCycleCap
            FROM dbo.CardExpenses
            WHERE BillingCycle IN @Cycles;";

        return await connection.QueryAsync<CardExpense>(sql, new { Cycles = cycles });
    }
}
