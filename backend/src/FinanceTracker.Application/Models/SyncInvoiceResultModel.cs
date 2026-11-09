namespace FinanceTracker.Application.Models;

public class SyncInvoiceResultModel
{
    public string Message { get; set; } = string.Empty;
    public decimal CurrentInvoiceTotal { get; set; }
}
