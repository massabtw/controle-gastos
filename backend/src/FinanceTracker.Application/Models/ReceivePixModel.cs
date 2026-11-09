namespace FinanceTracker.Application.Models;

public class ReceivePixModel
{
    public string Description { get; set; } = "Pix Recebido";
    public decimal Amount { get; set; }
    public string? Category { get; set; } = "Entrada Pix";
    public DateTime? TransactionDate { get; set; }
}
