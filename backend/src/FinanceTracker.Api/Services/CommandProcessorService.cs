using System.Globalization;
using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Infrastructure.Repositories;
using FinanceTracker.Api.Services.Ai;

namespace FinanceTracker.Api.Services;

public interface ICommandProcessorService
{
    Task<ChatCommandResponseDto> ProcessCommandAsync(string message);
}

public class CommandProcessorService : ICommandProcessorService
{
    private readonly IGroqExpenseParserService _parserService;
    private readonly IExpenseService _expenseService;
    private readonly IFinancialEngineService _financialEngineService;
    private readonly IProfileRepository _profileRepository;
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public CommandProcessorService(
        IGroqExpenseParserService parserService,
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IProfileRepository profileRepository)
    {
        _parserService = parserService;
        _expenseService = expenseService;
        _financialEngineService = financialEngineService;
        _profileRepository = profileRepository;
    }

    public async Task<ChatCommandResponseDto> ProcessCommandAsync(string message)
    {
        var parsed = await _parserService.ParseAsync(message);

        switch (parsed.Intent)
        {
            case "ADD_EXPENSE":
            {
                if (parsed.PaymentMethod == "PIX_CAIXINHA")
                {
                    var createdPix = await _expenseService.CreatePixCaixinhaExpenseAsync(new CreatePixCaixinhaDto
                    {
                        Description = parsed.Description,
                        Category = parsed.Category,
                        Amount = parsed.Amount,
                        DeductFromCycleCap = true,
                        TransactionDate = parsed.TransactionDate ?? DateTime.Now
                    });

                    var dashboardPix = await _financialEngineService.GetDashboardSummaryAsync();

                    var replyPix = $"💸 Pix (Caixinha) registrado: {createdPix.Description} (R$ {createdPix.Amount.ToString("N2", PtBrCulture)})\n" +
                                   $"🐷 Novo saldo Caixinha: R$ {dashboardPix.NubankCaixinhaBalance.ToString("N2", PtBrCulture)}\n" +
                                   $"📉 Teto Livre: R$ {dashboardPix.RemainingBudget.ToString("N2", PtBrCulture)} (R$ {dashboardPix.AvailablePerDay.ToString("N2", PtBrCulture)}/dia)";

                    return new ChatCommandResponseDto
                    {
                        Intent = "ADD_EXPENSE",
                        Reply = replyPix,
                        Dashboard = dashboardPix
                    };
                }

                var created = await _expenseService.CreateExpenseAsync(new CreateExpenseDto
                {
                    Description = parsed.Description,
                    Category = parsed.Category,
                    TotalAmount = parsed.Amount,
                    Installments = parsed.Installments,
                    TransactionDate = parsed.TransactionDate ?? DateTime.Now
                });

                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();

                string reply;
                if (parsed.Installments > 1)
                {
                    var installmentValue = created.FirstOrDefault()?.Amount ?? (parsed.Amount / parsed.Installments);
                    reply = $"✅ Registrado: {parsed.Description} (R$ {parsed.Amount.ToString("N2", PtBrCulture)} em {parsed.Installments}x de R$ {installmentValue.ToString("N2", PtBrCulture)}) [{parsed.Category}]\n" +
                            $"📉 Teto Livre: R$ {dashboard.RemainingBudget.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Novo limite diário: R$ {dashboard.AvailablePerDay.ToString("N2", PtBrCulture)}/dia ({dashboard.DaysUntilClosing}d restantes)";
                }
                else
                {
                    reply = $"✅ Registrado: {parsed.Description} (R$ {parsed.Amount.ToString("N2", PtBrCulture)}) [{parsed.Category}]\n" +
                            $"📉 Teto Livre: R$ {dashboard.RemainingBudget.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Novo limite diário: R$ {dashboard.AvailablePerDay.ToString("N2", PtBrCulture)}/dia ({dashboard.DaysUntilClosing}d restantes)";
                }

                return new ChatCommandResponseDto
                {
                    Intent = "ADD_EXPENSE",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            case "STATUS":
            {
                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();
                var pixCaixinhaInfo = dashboard.PixCaixinhaUsedInCycle > 0
                    ? $"\n💸 Pix Caixinha no Ciclo: R$ {dashboard.PixCaixinhaUsedInCycle.ToString("N2", PtBrCulture)}"
                    : "";

                var reply = $"📊 Ciclo {dashboard.BillingCycle} (Fecha {dashboard.CycleClosingDate:dd/MM} • Vence {dashboard.CycleDueDate:dd/MM})\n" +
                            $"💳 Fatura Cartão: R$ {dashboard.CurrentInvoiceTotal.ToString("N2", PtBrCulture)} / R$ {dashboard.CardHardCap.ToString("N2", PtBrCulture)}" +
                            pixCaixinhaInfo + "\n" +
                            $"🟢 Saldo no Teto: R$ {dashboard.RemainingBudget.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Limite Diário: R$ {dashboard.AvailablePerDay.ToString("N2", PtBrCulture)}/dia ({dashboard.DaysUntilClosing}d restantes)\n" +
                            $"🐷 Caixinha (115% CDI): R$ {dashboard.NubankCaixinhaBalance.ToString("N2", PtBrCulture)}\n" +
                            $"🛡️ Meta R$ {dashboard.MonthlyInvestmentGoal.ToString("N2", PtBrCulture)}: Intocada";

                return new ChatCommandResponseDto
                {
                    Intent = "STATUS",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            case "SYNC_INVOICE":
            {
                await _expenseService.SyncInvoiceTotalAsync(parsed.Amount);
                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();

                var reply = $"🔄 Fatura ajustada para R$ {dashboard.CurrentInvoiceTotal.ToString("N2", PtBrCulture)}!\n" +
                            $"📉 Teto Livre Restante: R$ {dashboard.RemainingBudget.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Limite Diário: R$ {dashboard.AvailablePerDay.ToString("N2", PtBrCulture)}/dia ({dashboard.DaysUntilClosing}d restantes)";

                return new ChatCommandResponseDto
                {
                    Intent = "SYNC_INVOICE",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            case "UPDATE_CAIXINHA":
            {
                await _profileRepository.UpdateCaixinhaBalanceAsync(parsed.Amount);
                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();

                var reply = $"🐷 Saldo da Caixinha atualizado para R$ {dashboard.NubankCaixinhaBalance.ToString("N2", PtBrCulture)}!\n" +
                            $"📈 Rendimento estimado (115% CDI): +R$ {dashboard.NubankCaixinhaMonthlyYield.ToString("N2", PtBrCulture)}/mês (+R$ {dashboard.NubankCaixinhaDailyYield.ToString("N2", PtBrCulture)}/dia)";

                return new ChatCommandResponseDto
                {
                    Intent = "UPDATE_CAIXINHA",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            case "SIMULATE":
            {
                var sim = await _financialEngineService.SimulatePurchaseAsync(new SimulatorRequestDto
                {
                    Amount = parsed.Amount,
                    Installments = parsed.Installments
                });

                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();
                var warning = sim.WillInvadeInvestmentGoal
                    ? $"\n⚠️ ALERTA: Esta compra invade sua Meta de Investimento em R$ {sim.InvadedAmount.ToString("N2", PtBrCulture)}!"
                    : "\n🛡️ Sua meta de investimento de R$ 1.000 continua segura.";

                var reply = $"🔮 Simulação: R$ {sim.Amount.ToString("N2", PtBrCulture)} em {sim.Installments}x de R$ {sim.InstallmentValue.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Limite diário: De R$ {sim.CurrentDailyBefore.ToString("N2", PtBrCulture)}/dia para R$ {sim.CurrentDailyAfter.ToString("N2", PtBrCulture)}/dia\n" +
                            $"💳 Teto restante ficaria: R$ {sim.RemainingBudgetAfter.ToString("N2", PtBrCulture)}" +
                            warning;

                return new ChatCommandResponseDto
                {
                    Intent = "SIMULATE",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            case "UNDO":
            {
                var undoResult = await _expenseService.UndoLatestExpenseAsync();
                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();

                var reply = $"↩️ {undoResult}\n" +
                            $"📉 Teto Livre Atualizado: R$ {dashboard.RemainingBudget.ToString("N2", PtBrCulture)}\n" +
                            $"🔥 Limite diário: R$ {dashboard.AvailablePerDay.ToString("N2", PtBrCulture)}/dia";

                return new ChatCommandResponseDto
                {
                    Intent = "UNDO",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }

            default:
            {
                var dashboard = await _financialEngineService.GetDashboardSummaryAsync();
                var reply = "❓ Não entendi o comando. Exemplos:\n" +
                            "• 'uber 25' ou 'almoço 34,90'\n" +
                            "• 'teclado 300 3x'\n" +
                            "• 'simular 400 4x'\n" +
                            "• '/status'\n" +
                            "• '/desfazer'";

                return new ChatCommandResponseDto
                {
                    Intent = "UNKNOWN",
                    Reply = reply,
                    Dashboard = dashboard
                };
            }
        }
    }
}
