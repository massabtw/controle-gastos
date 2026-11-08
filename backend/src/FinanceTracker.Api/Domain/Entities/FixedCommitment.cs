namespace FinanceTracker.Api.Domain.Entities;

public class FixedCommitment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string StartCycle { get; set; } = string.Empty; // Format: 'YYYY-MM'
    public int? TotalInstallments { get; set; } // null = fixed recurring indefinitely; int = fixed installments
    public bool IsActive { get; set; } = true;
}
