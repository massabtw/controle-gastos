namespace FinanceTracker.Application.Models;

public class SystemStatusModel
{
    public string Status { get; set; } = "ONLINE";
    public bool DatabaseConnected { get; set; }
    public string? DatabaseError { get; set; }
    public string ActiveBillingCycle { get; set; } = string.Empty;
    public DateTime CycleClosingDate { get; set; }
    public DateTime CycleDueDate { get; set; }
    public DateTime ServerTime { get; set; } = DateTime.Now;
}
