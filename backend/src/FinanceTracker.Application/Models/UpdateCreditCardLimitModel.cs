namespace FinanceTracker.Application.Models;

public class UpdateCreditCardLimitModel
{
    public decimal CreditCardLimit { get; set; }
    public decimal? CustomCardCap { get; set; }
}
