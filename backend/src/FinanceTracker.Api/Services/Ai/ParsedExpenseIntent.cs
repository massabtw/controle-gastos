using System.Text.Json.Serialization;

namespace FinanceTracker.Api.Services.Ai;

public class ParsedExpenseIntent
{
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = "UNKNOWN"; // "ADD_EXPENSE", "SIMULATE", "STATUS", "UNDO", "UNKNOWN"

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("installments")]
    public int Installments { get; set; } = 1;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Outros";

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = "CARD"; // "CARD" or "PIX_CAIXINHA"

    [JsonPropertyName("transactionDate")]
    public DateTime? TransactionDate { get; set; }
}
