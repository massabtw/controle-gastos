namespace FinanceTracker.Application.Models;

public class UpdateProfileResultModel
{
    public string Message { get; set; } = string.Empty;
    public FinancialProfileModel Profile { get; set; } = new();
}
