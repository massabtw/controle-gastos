using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Invoices;

public interface IInvoiceService
{
    Task<Result<InvoicePaymentModel?>> GetInvoicePaymentAsync(string? cycle);
    Task<Result<object>> TogglePaymentAsync(string? cycle);
    Task<Result<object>> InvestSurplusAsync(string? cycle);
}
