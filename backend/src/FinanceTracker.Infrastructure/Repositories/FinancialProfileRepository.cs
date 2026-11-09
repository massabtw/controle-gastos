using Dapper;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.UnitOfWorks;

namespace FinanceTracker.Infrastructure.Repositories;

public class FinancialProfileRepository : IFinancialProfileRepository
{
    private readonly UnitOfWorkApp _unitOfWork;

    public FinancialProfileRepository(UnitOfWorkApp unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FinancialProfileEntity> GetProfileAsync()
    {
        var sql = _unitOfWork.IsPostgres
            ? @"SELECT 
                    Id, Uuid,
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
                    TelegramAllowedChatId,
                    CreatedAt, UpdatedAt, Deleted
                FROM FinancialProfile
                WHERE Deleted = FALSE
                ORDER BY Id ASC
                LIMIT 1;"
            : @"SELECT TOP 1 
                    Id, Uuid,
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
                    TelegramAllowedChatId,
                    CreatedAt, UpdatedAt, Deleted
                FROM dbo.FinancialProfile
                WHERE Deleted = 0
                ORDER BY Id ASC;";

        var profile = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<FinancialProfileEntity>(sql, transaction: _unitOfWork.Transaction);

        if (profile == null)
        {
            profile = new FinancialProfileEntity();
        }

        return profile;
    }

    public async Task<bool> UpdateProfileAsync(FinancialProfileEntity profile)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FinancialProfile
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
                    TelegramAllowedChatId = @TelegramAllowedChatId,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @Id AND Deleted = FALSE;"
            : @"UPDATE dbo.FinancialProfile
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
                    TelegramAllowedChatId = @TelegramAllowedChatId,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = @Id AND Deleted = 0;";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, profile, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<bool> UpdateCaixinhaBalanceAsync(decimal balance)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FinancialProfile
                SET NubankCaixinhaBalance = @Balance,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);"
            : @"UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = @Balance,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { Balance = balance }, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<decimal> DepositCaixinhaAsync(decimal amount)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);

                SELECT NubankCaixinhaBalance FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1;"
            : @"UPDATE dbo.FinancialProfile
                SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);

                SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

        return await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Amount = amount }, _unitOfWork.Transaction);
    }

    public async Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap)
    {
        var sql = _unitOfWork.IsPostgres
            ? @"UPDATE FinancialProfile
                SET CreditCardLimit = @CreditCardLimit,
                    CustomCardCap = @CustomCardCap,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = (SELECT Id FROM FinancialProfile WHERE Deleted = FALSE ORDER BY Id ASC LIMIT 1);"
            : @"UPDATE dbo.FinancialProfile
                SET CreditCardLimit = @CreditCardLimit,
                    CustomCardCap = @CustomCardCap,
                    UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { CreditCardLimit = creditCardLimit, CustomCardCap = customCardCap }, _unitOfWork.Transaction);
        return affected > 0;
    }
}
