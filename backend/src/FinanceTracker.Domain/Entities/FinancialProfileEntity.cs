namespace FinanceTracker.Domain.Entities;

public class FinancialProfileEntity : Entity
{
    public decimal IncomeFirstTranche { get; set; } = 1200.00m;
    public decimal IncomeSecondTranche { get; set; } = 1000.00m;
    public decimal MonthlyInvestmentGoal { get; set; } = 1000.00m;
    public int ClosingDay { get; set; } = 8;
    public int DueDay { get; set; } = 16;
    public decimal CdiAnnualRate { get; set; } = 11.25m;

    public decimal NubankCaixinhaBalance { get; set; } = 0.00m;
    public decimal NubankCaixinhaCdiRate { get; set; } = 115.00m;

    public decimal CreditCardLimit { get; set; } = 2500.00m;
    public decimal? CustomCardCap { get; set; }

    public string? GroqApiKey { get; set; }
    public string? TelegramBotToken { get; set; }
    public long? TelegramAllowedChatId { get; set; }

    public decimal TotalIncome => IncomeFirstTranche + IncomeSecondTranche;
}
