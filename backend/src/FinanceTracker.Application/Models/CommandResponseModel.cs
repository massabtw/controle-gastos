namespace FinanceTracker.Application.Models;

public class CommandResponseModel
{
    public bool Success { get; set; }
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public object? Data { get; set; }
}
