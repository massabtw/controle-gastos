namespace FinanceTracker.Application.Models;

public class InvoiceTogglePaidResultModel
{
    public string Message { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal SurplusAmount { get; set; }
    public bool SurplusInvested { get; set; }
    public DateTime? PaidAt { get; set; }
}
