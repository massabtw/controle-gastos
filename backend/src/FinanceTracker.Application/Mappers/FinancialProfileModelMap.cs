using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Mappers;

public static class FinancialProfileModelMap
{
    public static FinancialProfileModel ToModel(this FinancialProfileEntity entity)
    {
        return new FinancialProfileModel
        {
            Id = entity.Id,
            Uuid = entity.Uuid,
            IncomeFirstTranche = entity.IncomeFirstTranche,
            IncomeSecondTranche = entity.IncomeSecondTranche,
            MonthlyInvestmentGoal = entity.MonthlyInvestmentGoal,
            ClosingDay = entity.ClosingDay,
            DueDay = entity.DueDay,
            CdiAnnualRate = entity.CdiAnnualRate,
            NubankCaixinhaBalance = entity.NubankCaixinhaBalance,
            NubankCaixinhaCdiRate = entity.NubankCaixinhaCdiRate,
            CreditCardLimit = entity.CreditCardLimit,
            CustomCardCap = entity.CustomCardCap
        };
    }

    public static FinancialProfileEntity ToEntity(this UpdateFinancialProfileModel model, FinancialProfileEntity existing)
    {
        existing.IncomeFirstTranche = model.IncomeFirstTranche;
        existing.IncomeSecondTranche = model.IncomeSecondTranche;
        existing.MonthlyInvestmentGoal = model.MonthlyInvestmentGoal;
        existing.ClosingDay = model.ClosingDay;
        existing.DueDay = model.DueDay;
        existing.CdiAnnualRate = model.CdiAnnualRate;
        existing.NubankCaixinhaBalance = model.NubankCaixinhaBalance;
        existing.NubankCaixinhaCdiRate = model.NubankCaixinhaCdiRate;
        existing.CreditCardLimit = model.CreditCardLimit;
        existing.CustomCardCap = model.CustomCardCap;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        return existing;
    }
}
