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
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                       CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                FROM FixedCommitments
                WHERE Id = @id AND Deleted = FALSE;"
            : @"SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                       CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                FROM dbo.FixedCommitments
                WHERE Id = @id AND Deleted = 0;";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<FixedCommitmentEntity>(sql, new { id }, _unitOfWork.Transaction);
    }

    public async Task<IEnumerable<FixedCommitmentEntity>> GetAllAsync(bool onlyActive = true)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                       CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                FROM FixedCommitments 
                WHERE Deleted = FALSE " + (onlyActive ? "AND IsActive = TRUE " : "") + "ORDER BY Id ASC;"
            : @"SELECT Id, Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                       CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                FROM dbo.FixedCommitments 
                WHERE Deleted = 0 " + (onlyActive ? "AND IsActive = 1 " : "") + "ORDER BY Id ASC;";

        return await _unitOfWork.Connection.QueryAsync<FixedCommitmentEntity>(sql, transaction: _unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(FixedCommitmentEntity entity)
    {
        if (entity.Uuid == Guid.Empty)
            entity.Uuid = Guid.NewGuid();

        var sql = _unitOfWork.IsPostgres
            ? @"INSERT INTO FixedCommitments (
                    Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                    CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Title, @Amount, @StartCycle, @TotalInstallments, @IsActive, 
                    @CommitmentType, @PaymentSource, @BillingDay, @LastPaidCycle, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE
                ) RETURNING Id;"
            : @"INSERT INTO dbo.FixedCommitments (
                    Uuid, Title, Amount, StartCycle, TotalInstallments, IsActive, 
                    CommitmentType, PaymentSource, BillingDay, LastPaidCycle, CreatedAt, UpdatedAt, Deleted
                ) VALUES (
                    @Uuid, @Title, @Amount, @StartCycle, @TotalInstallments, @IsActive, 
                    @CommitmentType, @PaymentSource, @BillingDay, @LastPaidCycle, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(sql, entity, _unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(FixedCommitmentEntity entity)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FixedCommitments
                SET Title = @Title,
                    Amount = @Amount,
                    StartCycle = @StartCycle,
                    TotalInstallments = @TotalInstallments,
                    IsActive = @IsActive,
                    CommitmentType = @CommitmentType,
                    PaymentSource = @PaymentSource,
                    BillingDay = @BillingDay,
                    LastPaidCycle = @LastPaidCycle,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @Id AND Deleted = FALSE;"
            : @"UPDATE dbo.FixedCommitments
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
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FixedCommitments 
                SET Deleted = TRUE, UpdatedAt = CURRENT_TIMESTAMP 
                WHERE Id = @id AND Deleted = FALSE;"
            : @"UPDATE dbo.FixedCommitments 
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

        if (_unitOfWork.IsPostgres)
        {
            if (isAlreadyPaid)
            {
                var sql = @"
                    " + (isCaixinha ? @"
                    UPDATE FinancialProfile
                    SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);
                    " : "") + @"

                    UPDATE FixedCommitments
                    SET LastPaidCycle = NULL,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Id = @Id AND Deleted = FALSE;

                    SELECT NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;";

                var balance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount }, _unitOfWork.Transaction);
                return (true, false, balance);
            }
            else
            {
                var sql = @"
                    " + (isCaixinha ? @"
                    UPDATE FinancialProfile
                    SET NubankCaixinhaBalance = CASE 
                        WHEN NubankCaixinhaBalance - @Amount < 0 THEN 0 
                        ELSE NubankCaixinhaBalance - @Amount 
                    END,
                    UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);
                    " : "") + @"

                    UPDATE FixedCommitments
                    SET LastPaidCycle = @Cycle,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Id = @Id AND Deleted = FALSE;

                    SELECT NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;";

                var balance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount, Cycle = cycle }, _unitOfWork.Transaction);
                return (true, true, balance);
            }
        }
        else
        {
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
}
