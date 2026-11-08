using Dapper;
using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Infrastructure.Data;

namespace FinanceTracker.Api.Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProfileRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FinancialProfile> GetProfileAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 
                Id, 
                IncomeFirstTranche, 
                IncomeSecondTranche, 
                MonthlyInvestmentGoal, 
                ClosingDay, 
                DueDay, 
                CdiAnnualRate,
                NubankCaixinhaBalance,
                NubankCaixinhaCdiRate,
                CreditCardLimit,
                CustomCardCap,
                GroqApiKey,
                TelegramBotToken,
                TelegramAllowedChatId
            FROM dbo.FinancialProfile
            ORDER BY Id ASC;";

        var profile = await connection.QueryFirstOrDefaultAsync<FinancialProfile>(sql);

        if (profile == null)
        {
            // Fallback default
            profile = new FinancialProfile();
        }

        return profile;
    }

    public async Task<bool> UpdateProfileAsync(FinancialProfile profile)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET IncomeFirstTranche = @IncomeFirstTranche,
                IncomeSecondTranche = @IncomeSecondTranche,
                MonthlyInvestmentGoal = @MonthlyInvestmentGoal,
                ClosingDay = @ClosingDay,
                DueDay = @DueDay,
                CdiAnnualRate = @CdiAnnualRate,
                NubankCaixinhaBalance = @NubankCaixinhaBalance,
                NubankCaixinhaCdiRate = @NubankCaixinhaCdiRate,
                CreditCardLimit = @CreditCardLimit,
                CustomCardCap = @CustomCardCap,
                GroqApiKey = @GroqApiKey,
                TelegramBotToken = @TelegramBotToken,
                TelegramAllowedChatId = @TelegramAllowedChatId
            WHERE Id = @Id;";

        var affected = await connection.ExecuteAsync(sql, profile);
        return affected > 0;
    }

    public async Task<bool> UpdateCaixinhaBalanceAsync(decimal balance)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET NubankCaixinhaBalance = @Balance
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile ORDER BY Id ASC);";

        var affected = await connection.ExecuteAsync(sql, new { Balance = balance });
        return affected > 0;
    }

    public async Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET CreditCardLimit = @CreditCardLimit,
                CustomCardCap = @CustomCardCap
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile ORDER BY Id ASC);";

        var affected = await connection.ExecuteAsync(sql, new { CreditCardLimit = creditCardLimit, CustomCardCap = customCardCap });
        return affected > 0;
    }

    public async Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle)
    {
        using var connection = _connectionFactory.CreateConnection();
        var commitment = await GetFixedCommitmentByIdAsync(id);
        if (commitment == null) return (false, false, 0);

        var isAlreadyPaid = string.Equals(commitment.LastPaidCycle, cycle, StringComparison.OrdinalIgnoreCase);
        var isCaixinha = string.Equals(commitment.PaymentSource, "PIX_CAIXINHA", StringComparison.OrdinalIgnoreCase);

        if (isAlreadyPaid)
        {
            // Unmark: LastPaidCycle = NULL and refund if Caixinha
            var sql = @"
                BEGIN TRANSACTION;

                " + (isCaixinha ? @"
                UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile ORDER BY Id ASC);
                " : "") + @"

                UPDATE dbo.FixedCommitments
                SET LastPaidCycle = NULL
                WHERE Id = @Id;

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile ORDER BY Id ASC;

                COMMIT TRANSACTION;";

            var balance = await connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount });
            return (true, false, balance);
        }
        else
        {
            // Mark: LastPaidCycle = @Cycle and deduct if Caixinha
            var sql = @"
                BEGIN TRANSACTION;

                " + (isCaixinha ? @"
                UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = CASE 
                    WHEN NubankCaixinhaBalance - @Amount < 0 THEN 0 
                    ELSE NubankCaixinhaBalance - @Amount 
                END
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile ORDER BY Id ASC);
                " : "") + @"

                UPDATE dbo.FixedCommitments
                SET LastPaidCycle = @Cycle
                WHERE Id = @Id;

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile ORDER BY Id ASC;

                COMMIT TRANSACTION;";

            var balance = await connection.ExecuteScalarAsync<decimal>(sql, new { Id = id, Amount = commitment.Amount, Cycle = cycle });
            return (true, true, balance);
        }
    }

    public async Task<bool> MarkCommitmentPaidFromCaixinhaAsync(int id, string cycle)
    {
        var result = await ToggleCommitmentPaidAsync(id, cycle);
        return result.success;
    }

    public async Task<IEnumerable<FixedCommitment>> GetFixedCommitmentsAsync(bool onlyActive = true)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT Id, Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay, LastPaidCycle
            FROM dbo.FixedCommitments " +
            (onlyActive ? "WHERE IsActive = 1 " : "") +
            "ORDER BY Id ASC;";

        return await connection.QueryAsync<FixedCommitment>(sql);
    }

    public async Task<FixedCommitment?> GetFixedCommitmentByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay, LastPaidCycle
            FROM dbo.FixedCommitments
            WHERE Id = @Id;";

        return await connection.QueryFirstOrDefaultAsync<FixedCommitment>(sql, new { Id = id });
    }

    public async Task<int> AddFixedCommitmentAsync(FixedCommitment commitment)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.FixedCommitments (Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay, LastPaidCycle)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Amount, @StartCycle, @TotalInstallments, @IsActive, @CommitmentType, @PaymentSource, @BillingDay, @LastPaidCycle);";

        return await connection.ExecuteScalarAsync<int>(sql, commitment);
    }

    public async Task<bool> UpdateFixedCommitmentAsync(FixedCommitment commitment)
    {
        using var connection = _connectionFactory.CreateConnection();
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
                LastPaidCycle = @LastPaidCycle
            WHERE Id = @Id;";

        var affected = await connection.ExecuteAsync(sql, commitment);
        return affected > 0;
    }

    public async Task<bool> DeleteFixedCommitmentAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"DELETE FROM dbo.FixedCommitments WHERE Id = @Id;";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}
