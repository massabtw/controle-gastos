using Dapper;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.UnitOfWorks;

namespace FinanceTracker.Infrastructure.Repositories;

public class FixedCommitmentRepository : IFixedCommitmentRepository
{
    private readonly UnitOfWorkApp _unitOfWork;

    public FixedCommitmentRepository(UnitOfWorkApp unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FixedCommitmentEntity?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                   CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
            FROM dbo.FixedCommitments
            WHERE Id = @id AND Deleted = 0;";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<FixedCommitmentEntity>(sql, new { id }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<FixedCommitmentEntity>> GetAllAsync(bool onlyActive = true)
    {
        var sql = @"
            SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                   CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
            FROM dbo.FixedCommitments 
            WHERE Deleted = 0 " +
            (onlyActive ? "AND IsActive = 1 " : "") +
            "ORDER BY Id ASC;";

        return await _unitOfWork.Connection.QueryAsync<FixedCommitmentEntity>(sql, transaction: _unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(FixedCommitmentEntity entity)
    {
        const string sql = @"
            INSERT INTO dbo.FixedCommitments (
                Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
            ) VALUES (
                @Uuid, @Title, @Amount, @StartCycle, @TotalInstallments, @IsActive, 
                @CommitmentType, @PaymentSource, @BillingDay, @LastPaidCycle, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
            );
            SELECT CAST(SCOPE_IDENTITY() as int);";

        if (entity.Uuid == Guid.Empty)
            entity.Uuid = Guid.NewGuid();

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(sql, entity, _unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(FixedCommitmentEntity entity)
    {
        const string sql = @"
            UPDATE dbo.FixedCommitments
            SET Title = @Title,
                Amount = @Amount,
                StartCycle = @StartCycle,
                TotalInstallments = @TotalInstallments,
                IsActive = @IsActive,
                CommitmentType = @CommitmentType,
                PaymentSource = @PaymentSource,
                BillingDay = @BillingDay,
                LastPaidCycle = @LastPaidCycle,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = @Id AND Deleted = 0;";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, entity, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
            UPDATE dbo.FixedCommitments 
            SET Deleted = 1, UpdatedAt = SYSDATETIMEOFFSET() 
            WHERE Id = @id AND Deleted = 0;";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { id }, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle)
    {
        var commitment = await GetByIdAsync(id);
        if (commitment == null) return (false, false, 0);

        var isAlreadyPaid = string.Equals(commitment.LastPaidCycle, cycle, StringComparison.OrdinalIgnoreCase);
        var isCaixinha = string.Equals(commitment.PaymentSource, "PIX_CAIXINHA", StringComparison.OrdinalIgnoreCase);

        if (isAlreadyPaid)
        {
            var sql = @"
                " + (isCaixinha ? @"
                UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);
                " : "") + @"

                UPDATE dbo.FixedCommitments
                SET LastPaidCycle = NULL,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = @Id AND Deleted = 0;

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

            var balance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount }, _unitOfWork.Transaction);
            return (true, false, balance);
        }
        else
        {
            var sql = @"
                " + (isCaixinha ? @"
                UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = CASE 
                    WHEN NubankCaixinhaBalance - @Amount < 0 THEN 0 
                    ELSE NubankCaixinhaBalance - @Amount 
                END,
                UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);
                " : "") + @"

                UPDATE dbo.FixedCommitments
                SET LastPaidCycle = @Cycle,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = @Id AND Deleted = 0;

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

            var balance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount, Cycle = cycle }, _unitOfWork.Transaction);
            return (true, true, balance);
        }
    }
}
