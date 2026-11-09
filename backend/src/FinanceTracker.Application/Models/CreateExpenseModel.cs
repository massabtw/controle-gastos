namespace FinanceTracker.Application.Models;

public class CreateExpenseModel
{
    private decimal _amount;

    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";

    public decimal Amount
    {
        get => _amount != 0 ? _amount : TotalAmount;
        set => _amount = value;
    }

    public decimal TotalAmount { get; set; }
    public int Installments { get; set; } = 1;
    public DateTime? TransactionDate { get; set; }
    public string? PaymentMethod { get; set; }
    public bool? DeductFromCycleCap { get; set; }
}
