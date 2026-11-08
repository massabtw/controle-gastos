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

    public async Task<IEnumerable<FixedCommitment>> GetFixedCommitmentsAsync(bool onlyActive = true)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT Id, Title, Amount, StartCycle, TotalInstallments, IsActive
            FROM dbo.FixedCommitments " +
            (onlyActive ? "WHERE IsActive = 1 " : "") +
            "ORDER BY Id ASC;";

        return await connection.QueryAsync<FixedCommitment>(sql);
    }

    public async Task<FixedCommitment?> GetFixedCommitmentByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Title, Amount, StartCycle, TotalInstallments, IsActive
            FROM dbo.FixedCommitments
            WHERE Id = @Id;";

        return await connection.QueryFirstOrDefaultAsync<FixedCommitment>(sql, new { Id = id });
    }

    public async Task<int> AddFixedCommitmentAsync(FixedCommitment commitment)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.FixedCommitments (Title, Amount, StartCycle, TotalInstallments, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Amount, @StartCycle, @TotalInstallments, @IsActive);";

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
                IsActive = @IsActive
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
