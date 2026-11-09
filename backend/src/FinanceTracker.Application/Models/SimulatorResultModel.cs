namespace FinanceTracker.Application.Models;

public class SimulatorResultModel
{
    public decimal TotalAmount { get; set; }
    public int Installments { get; set; }
    public decimal InstallmentValue { get; set; }
    public bool IsViable { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public decimal CurrentCaixinhaYield { get; set; }
    public List<SimulatorMonthImpactModel> MonthlyImpacts { get; set; } = new();
}
