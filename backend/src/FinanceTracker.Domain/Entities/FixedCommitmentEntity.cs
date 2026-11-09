namespace FinanceTracker.Domain.Entities;

public class FixedCommitmentEntity : Entity
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string StartCycle { get; set; } = string.Empty;
    public int? TotalInstallments { get; set; }
    public bool IsActive { get; set; } = true;
    public string CommitmentType { get; set; } = "PIX_COMMITMENT";
    public string PaymentSource { get; set; } = "PIX_CAIXINHA";
    public int? BillingDay { get; set; } = 1;
    public string? LastPaidCycle { get; set; }
}
