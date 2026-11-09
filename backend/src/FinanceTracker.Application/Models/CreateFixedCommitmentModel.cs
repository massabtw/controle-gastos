namespace FinanceTracker.Application.Models;

public class CreateFixedCommitmentModel
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? StartCycle { get; set; }
    public int? TotalInstallments { get; set; }
    public bool? IsActive { get; set; }
    public string? CommitmentType { get; set; } = "PIX_COMMITMENT";
    public string? PaymentSource { get; set; } = "PIX_CAIXINHA";
    public int? BillingDay { get; set; } = 1;
}
