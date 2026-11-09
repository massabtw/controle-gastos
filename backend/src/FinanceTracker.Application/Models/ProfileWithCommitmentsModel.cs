namespace FinanceTracker.Application.Models;

public class ProfileWithCommitmentsModel
{
    public FinancialProfileModel Profile { get; set; } = new();
    public List<FixedCommitmentModel> Commitments { get; set; } = new();
}
