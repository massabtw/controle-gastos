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
        const string sql = @"
            SELECT TOP 1 
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
                TelegramAllowedChatId = @TelegramAllowedChatId,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = @Id AND Deleted = 0;";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, profile, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<bool> UpdateCaixinhaBalanceAsync(decimal balance)
    {
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET NubankCaixinhaBalance = @Balance,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { Balance = balance }, _unitOfWork.Transaction);
        return affected > 0;
    }

    public async Task<decimal> DepositCaixinhaAsync(decimal amount)
    {
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET NubankCaixinhaBalance = NubankCaixinhaBalance + @Amount,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);

            SELECT TOP 1 NubankCaixinhaBalance FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC;";

        return await _unitOfWork.Connection.ExecuteScalarAsync<decimal>(sql, new { Amount = amount }, _unitOfWork.Transaction);
    }

    public async Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap)
    {
        const string sql = @"
            UPDATE dbo.FinancialProfile
            SET CreditCardLimit = @CreditCardLimit,
                CustomCardCap = @CustomCardCap,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE Id = (SELECT TOP 1 Id FROM dbo.FinancialProfile WHERE Deleted = 0 ORDER BY Id ASC);";

        var affected = await _unitOfWork.Connection.ExecuteAsync(sql, new { CreditCardLimit = creditCardLimit, CustomCardCap = customCardCap }, _unitOfWork.Transaction);
        return affected > 0;
    }
}
