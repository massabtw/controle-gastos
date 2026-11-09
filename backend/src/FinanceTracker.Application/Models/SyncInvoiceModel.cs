namespace FinanceTracker.Application.Models;

public class SyncInvoiceModel
{
    public decimal TargetTotal { get; set; }
    public string? Cycle { get; set; }
}
