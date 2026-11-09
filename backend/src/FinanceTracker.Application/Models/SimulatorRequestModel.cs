namespace FinanceTracker.Application.Models;

public class SimulatorRequestModel
{
    public decimal Amount { get; set; }
    public int Installments { get; set; } = 1;
    public string? Description { get; set; }
    public string? TargetCycle { get; set; }
}
