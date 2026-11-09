using FinanceTracker.Application.Models;
using FinanceTracker.Domain.Results;

namespace FinanceTracker.Application.Interfaces.Invoices;

public interface IInvoiceService
{
    Task<Result<InvoicePaymentModel?>> GetInvoicePaymentAsync(string? cycle);
    Task<Result<InvoiceTogglePaidResultModel>> TogglePaymentAsync(string? cycle);
    Task<Result<InvestSurplusResultModel>> InvestSurplusAsync(string? cycle);
}
