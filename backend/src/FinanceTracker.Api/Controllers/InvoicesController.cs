using FinanceTracker.Api.Infrastructure.Repositories;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IProfileRepository _profileRepository;
    private readonly IFinancialEngineService _financialEngineService;

    public InvoicesController(
        IProfileRepository profileRepository,
        IFinancialEngineService financialEngineService)
    {
        _profileRepository = profileRepository;
        _financialEngineService = financialEngineService;
    }

    [HttpPost("{cycle}/toggle-paid")]
    public async Task<IActionResult> TogglePaid(string cycle)
    {
        var summary = await _financialEngineService.GetDashboardSummaryAsync(cycle);
        var profile = await _profileRepository.GetProfileAsync();

        var (isPaid, paidAmount, surplusAmount, surplusInvested, paidAt) =
            await _profileRepository.ToggleInvoicePaymentAsync(
                cycle,
                summary.CurrentInvoiceTotal,
                profile.IncomeSecondTranche
            );

        return Ok(new
        {
            message = isPaid ? "Fatura marcada como PAGA!" : "Pagamento da fatura desmarcado.",
            cycle,
            isPaid,
            paidAmount,
            surplusAmount,
            surplusInvested,
            paidAt
        });
    }

    [HttpPost("{cycle}/invest-surplus")]
    public async Task<IActionResult> InvestSurplus(string cycle)
    {
        var (success, investedAmount, newBalance) = await _profileRepository.InvestInvoiceSurplusAsync(cycle);
        if (!success)
        {
            return BadRequest(new { message = "Não há sobra elegível para investir nesta fatura." });
        }

        return Ok(new
        {
            message = $"Sobra de R$ {investedAmount:N2} enviada com sucesso para a Caixinha Nubank (115% CDI)!",
            cycle,
            investedAmount,
            newCaixinhaBalance = newBalance
        });
    }
}
