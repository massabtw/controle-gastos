namespace FinanceTracker.Application.Models;

public class UpdateCardLimitResultModel
{
    public string Message { get; set; } = string.Empty;
    public decimal CreditCardLimit { get; set; }
    public decimal? CustomCardCap { get; set; }
}
