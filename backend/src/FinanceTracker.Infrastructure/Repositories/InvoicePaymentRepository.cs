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
        const string sql = @"
            SELECT Id, Uuid, BillingCycle, IsPaid, PaidAmount, PaidAt, SurplusAmount, SurplusInvested, CreatedAt, UpdatedAt, Deleted
            FROM dbo.InvoicePayments
            WHERE BillingCycle = @cycle AND Deleted = 0;";

        return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<InvoicePaymentEntity>(sql, new { cycle }, _unitOfWork.Transaction);
    }

    public async Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> TogglePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary)
    {
        var current = await GetByCycleAsync(cycle);

        if (current != null && current.IsPaid)
        {
            const string unmarkSql = @"
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
                SurplusInvested = current.SurplusInvested ? 1 : 0,
                SurplusAmount = current.SurplusAmount
            }, _unitOfWork.Transaction);

            return (false, current.PaidAmount, current.SurplusAmount, false, null);
        }
        else
        {
            var paidAmount = currentInvoiceTotal;
            var surplus = Math.Max(0, secondTrancheSalary - paidAmount);
            var paidAt = DateTime.Now;

            const string markSql = @"
                MERGE dbo.InvoicePayments AS target
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
            const string getBalSql = "SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";
            var bal = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(getBalSql, transaction: _unitOfWork.Transaction);
            return (false, 0, bal);
        }

        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET NubankCaixinhaBalance = NubankCaixinhaBalance + @SurplusAmount,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);

            UPDATE dbo.InvoicePayments
            SET SurplusInvested = 1,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE BillingCycle = @Cycle AND Deleted = 0;

            SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

        var newBalance = await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Cycle = cycle, SurplusAmount = current.SurplusAmount }, _unitOfWork.Transaction);
        return (true, current.SurplusAmount, newBalance);
    }
}
