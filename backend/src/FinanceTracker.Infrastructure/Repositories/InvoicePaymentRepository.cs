using Dapper;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.UnitOfWorks;

namespace FinanceTracker.Infrastructure.Repositories;

public class InvoicePaymentRepository : IInvoicePaymentRepository
{
    private readonly UnitOfWorkApp _unitOfWork;

    public InvoicePaymentRepository(UnitOfWorkApp unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoicePaymentEntity?> GetByCycleAsync(string cycle)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT Id, Uuid, BillingCycle, IsPaid, PaidAmount, PaidAt, SurplusAmount, SurplusInvested, CreatedAt, UpdatedAt, Deleted
                FROM InvoicePayments
                WHERE BillingCycle = @cycle AND Deleted = FALSE;"
            : @"SELECT Id, Uuid, BillingCycle, IsPaid, PaidAmount, PaidAt, SurplusAmount, SurplusInvested, CreatedAt, UpdatedAt, Deleted
                FROM dbo.InvoicePayments
                WHERE BillingCycle = @cycle AND Deleted = 0;";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<InvoicePaymentEntity>(sql, new { cycle }, _unitOfWork.Transaction);
    }

    public async Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> TogglePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary)
    {
        var current = await GetByCycleAsync(cycle);

        if (current != null && current.IsPaid)
        {
            var unmarkSql = _unitOfWork.IsPostgres
                ? @"
                    DO $$
                    BEGIN
                        IF @SurplusInvested AND @SurplusAmount > 0 THEN
                            UPDATE FinancialProfile
                            SET NubankCaixinhaBalance = GREATEST(0, NubankCaixinhaBalance - @SurplusAmount),
                                UpdatedAt = CURRENT_TIMESTAMP
                            WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);
                        END IF;
                    END $$;

                    UPDATE InvoicePayments
                    SET IsPaid = FALSE, SurplusInvested = FALSE, UpdatedAt = CURRENT_TIMESTAMP
                    WHERE BillingCycle = @Cycle AND Deleted = FALSE;"
                : @"
                    IF @SurplusInvested = 1 AND @SurplusAmount > 0
                    BEGIN
                        UPDATE dbo.FinancialProfile
                        SET NubankCaixinhaBalance = CASE 
                            WHEN NubankCaixinhaBalance - @SurplusAmount < 0 THEN 0 
                            ELSE NubankCaixinhaBalance - @SurplusAmount 
                        END,
                        UpdatedAt = SYSDATETIMEOFFSET()
                        WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);
                    END

                    UPDATE dbo.InvoicePayments
                    SET IsPaid = 0, SurplusInvested = 0, UpdatedAt = SYSDATETIMEOFFSET()
                    WHERE BillingCycle = @Cycle AND Deleted = 0;";

            await _unitOfWork.Connection.ExecuteAsync(unmarkSql, new
            {
                Cycle = cycle,
                SurplusInvested = current.SurplusInvested,
                SurplusAmount = current.SurplusAmount
            }, _unitOfWork.Transaction);

            return (false, current.PaidAmount, current.SurplusAmount, false, null);
        }
        else
        {
            var paidAmount = currentInvoiceTotal;
            var surplus = Math.Max(0, secondTrancheSalary - paidAmount);
            var paidAt = DateTime.UtcNow;

            var markSql = _unitOfWork.IsPostgres
                ? @"INSERT INTO InvoicePayments (
                        Uuid, BillingCycle, IsPaid, PaidAmount, PaidAt, SurplusAmount, SurplusInvested, CreatedAt, UpdatedAt, Deleted
                    ) VALUES (
                        gen_random_uuid(), @Cycle, TRUE, @PaidAmount, @PaidAt, @SurplusAmount, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE
                    )
                    ON CONFLICT (BillingCycle) DO UPDATE
                    SET IsPaid = TRUE,
                        PaidAmount = @PaidAmount,
                        PaidAt = @PaidAt,
                        SurplusAmount = @SurplusAmount,
                        SurplusInvested = FALSE,
                        UpdatedAt = CURRENT_TIMESTAMP,
                        Deleted = FALSE;"
                : @"MERGE dbo.InvoicePayments AS target
                    USING (SELECT @Cycle AS BillingCycle) AS source
                    ON target.BillingCycle = source.BillingCycle AND target.Deleted = 0
                    WHEN MATCHED THEN
                        UPDATE SET IsPaid = 1,
                                   PaidAmount = @PaidAmount,
                                   PaidAt = @PaidAt,
                                   SurplusAmount = @SurplusAmount,
                                   SurplusInvested = 0,
                                   UpdatedAt = SYSDATETIMEOFFSET()
                    WHEN NOT MATCHED THEN
                        INSERT (Uuid, BillingCycle, IsPaid, PaidAmount, PaidAt, SurplusAmount, SurplusInvested, CreatedAt, UpdatedAt, Deleted)
                        VALUES (NEWID(), @Cycle, 1, @PaidAmount, @PaidAt, @SurplusAmount, 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0);";

            await _unitOfWork.Connection.ExecuteAsync(markSql, new
            {
                Cycle = cycle,
                PaidAmount = paidAmount,
                PaidAt = paidAt,
                SurplusAmount = surplus
            }, _unitOfWork.Transaction);

            return (true, paidAmount, surplus, false, paidAt);
        }
    }

    public async Task<(bool success, decimal surplusInvestedAmount, decimal newCaixinhaBalance)> InvestSurplusAsync(string cycle)
    {
        var current = await GetByCycleAsync(cycle);
        if (current == null || !current.IsPaid || current.SurplusInvested || current.SurplusAmount <= 0)
        {
            var balSql = _unitOfWork.IsPostgres
                ? "SELECT NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;"
                : "SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

            var bal = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(balSql, transaction: _unitOfWork.Transaction);
            return (false, 0, bal);
        }

        var getProfileSql = _unitOfWork.IsPostgres
            ? "SELECT IncomeSecondTranche, NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;"
            : "SELECT TOP 1 IncomeSecondTranche, NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

        var profile = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<(decimal IncomeSecondTranche, decimal NubankCaixinhaBalance)>(getProfileSql, transaction: _unitOfWork.Transaction);

        var surplus = Math.Max(0, profile.IncomeSecondTranche - current.PaidAmount);
        if (surplus <= 0)
        {
            return (false, 0, profile.NubankCaixinhaBalance);
        }

        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @SurplusAmount,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);

                UPDATE InvoicePayments
                SET SurplusInvested = TRUE,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE BillingCycle = @Cycle AND Deleted = FALSE;

                SELECT NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;"
            : @"UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @SurplusAmount,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);

                UPDATE dbo.InvoicePayments
                SET SurplusInvested = 1,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE BillingCycle = @Cycle AND Deleted = 0;

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

        var newBalance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Cycle = cycle, SurplusAmount = surplus }, _unitOfWork.Transaction);
        return (true, surplus, newBalance);
    }
}
