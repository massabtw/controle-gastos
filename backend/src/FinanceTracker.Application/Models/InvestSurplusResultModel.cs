namespace FinanceTracker.Application.Models;

public class InvestSurplusResultModel
{
    public string Message { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public decimal InvestedAmount { get; set; }
    public decimal SurplusInvestedAmount { get; set; }
    public decimal NewCaixinhaBalance { get; set; }
}
