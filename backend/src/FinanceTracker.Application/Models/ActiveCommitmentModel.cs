namespace FinanceTracker.Application.Models;

public class ActiveCommitmentModel
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string InstallmentStatus { get; set; } = string.Empty;
    public string CommitmentType { get; set; } = "PIX_COMMITMENT";
    public string PaymentSource { get; set; } = "PIX_CAIXINHA";
    public int? BillingDay { get; set; } = 1;
    public string? LastPaidCycle { get; set; }
    public bool IsPaidForCycle { get; set; }
}
