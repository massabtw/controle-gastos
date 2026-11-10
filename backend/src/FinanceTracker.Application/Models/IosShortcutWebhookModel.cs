namespace FinanceTracker.Application.Models;

public class IosShortcutWebhookModel
{
    public string? SecretPin { get; set; }
    public string? Merchant { get; set; }
    public object? Amount { get; set; }
    public string? RawText { get; set; }
    public string? PaymentMethod { get; set; }
    public int? Installments { get; set; }
    public bool? DeductFromCycleCap { get; set; }
}
