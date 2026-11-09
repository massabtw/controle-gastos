namespace FinanceTracker.Application.Models;

public class CaixinhaOperationResultModel
{
    public string Message { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Amount { get; set; }
}
