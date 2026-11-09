namespace FinanceTracker.Domain.Http.Groq.Models;

public class ParsedExpenseIntentModel
{
    public string Intent { get; set; } = "UNKNOWN";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Installments { get; set; } = 1;
    public string Category { get; set; } = "Outros";
    public string PaymentMethod { get; set; } = "CARD";
    public bool DeductFromCycleCap { get; set; } = true;
    public decimal Confidence { get; set; } = 1.0m;
    public DateTime? TransactionDate { get; set; }
}
