using FinanceTracker.Application.Interfaces.Commands;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Interfaces.Invoices;
using FinanceTracker.Application.Interfaces.Profile;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Groq.Models;
using FinanceTracker.Domain.Results;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Commands;

public class CommandProcessorService : ICommandProcessorService
{
    private readonly IGroqService _groqService;
    private readonly IExpenseService _expenseService;
    private readonly IFinancialEngineService _financialEngineService;
    private readonly IFinancialProfileService _profileService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<CommandProcessorService> _logger;

    public CommandProcessorService(
        IGroqService groqService,
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IFinancialProfileService profileService,
        IInvoiceService invoiceService,
        ILogger<CommandProcessorService> logger)
    {
        _groqService = groqService;
        _expenseService = expenseService;
        _financialEngineService = financialEngineService;
        _profileService = profileService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<Result<CommandResponseModel>> ProcessCommandAsync(string input)
    {
        try
        {
            if (string.Equals(input?.Trim(), "/start", StringComparison.OrdinalIgnoreCase))
            {
                return new CommandResponseModel
                {
                    Success = true,
                    Intent = "START",
                    Reply = "👋 Olá! Sou o seu assistente financeiro do BurnTracker.\n" +
                            "Aqui você controla seus gastos do cartão e saídas da caixinha em tempo real!\n\n" +
                            "Comandos e exemplos que você pode mandar:\n" +
                            "• Gasto Cartão: 'uber 25', 'almoço 34,90', 'teclado 300 3x'\n" +
                            "• Pix Caixinha: 'pix 40 corte', 'paguei 25 no pix lanche'\n" +
                            "• Receber Pix: 'recebi 50 pix do joao'\n" +
                            "• Adicionar Caixinha: '+10 caixinha'\n" +
                            "• Ajustar Fatura: 'fatura atual 350'\n" +
                            "• Liquidar Fatura: 'paguei a fatura' ou '/pagarfatura'\n" +
                            "• Reinvestir Sobra: 'investir sobra'\n" +
                            "• Atualizar Caixinha: 'saldo caixinha 4500'\n" +
                            "• Simulação: '/simular 300 3x'\n" +
                            "• Resumo do Ciclo: '/status'\n" +
                            "• Desfazer Último: '/desfazer'"
                };
            }

            var safeInput = input ?? string.Empty;
            var parsed = await _groqService.ParseExpenseIntentAsync(safeInput);

            if (parsed == null || parsed.Intent == "UNKNOWN" || parsed.Confidence < 0.6m)
            {
                parsed = RegexExpenseParser.Parse(safeInput);
            }

            return parsed.Intent switch
            {
                "ADD_EXPENSE" => await HandleAddExpenseAsync(parsed),
                "RECEIVE_PIX" => await HandleReceivePixAsync(parsed),
                "DEPOSIT_CAIXINHA" => await HandleDepositCaixinhaAsync(parsed),
                "STATUS" => await HandleStatusAsync(),
                "SIMULATE" => await HandleSimulateAsync(parsed),
                "UNDO" => await HandleUndoAsync(),
                "PAY_INVOICE" => await HandlePayInvoiceAsync(),
                "INVEST_SURPLUS" => await HandleInvestSurplusAsync(),
                "SYNC_INVOICE" => await HandleSyncInvoiceAsync(parsed),
                "UPDATE_CAIXINHA" => await HandleUpdateCaixinhaAsync(parsed),
                _ => new CommandResponseModel
                {
                    Success = false,
                    Intent = "UNKNOWN",
                    Reply = "Não entendi o comando. Exemplos:\n• 'uber 25'\n• 'pix 40 corte'\n• 'recebi 50 pix do joao'\n• '+10 caixinha'\n• 'paguei a fatura'\n• 'fatura atual 350'\n• 'saldo caixinha 4500'\n• 'simular 300 3x'\n• '/status'\n• '/desfazer'"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command {Input}", input);
            return new CommandResponseModel
            {
                Success = false,
                Intent = "ERROR",
                Reply = "Ocorreu um erro ao processar sua solicitação."
            };
        }
    }

    private async Task<CommandResponseModel> HandleAddExpenseAsync(ParsedExpenseIntentModel parsed)
    {
        var result = await _expenseService.CreateExpenseAsync(new CreateExpenseModel
        {
            Description = parsed.Description,
            Amount = parsed.Amount,
            TotalAmount = parsed.Amount,
            Category = parsed.Category,
            Installments = parsed.Installments,
            PaymentMethod = parsed.PaymentMethod,
            DeductFromCycleCap = parsed.DeductFromCycleCap
        });

        if (!result.IsSuccess)
        {
            return new CommandResponseModel
            {
                Success = false,
                Intent = "ADD_EXPENSE",
                Reply = $"Erro ao registrar despesa: {result.Error}"
            };
        }

        var dashboardResult = await _financialEngineService.GetDashboardSummaryAsync();
        var reply = parsed.PaymentMethod == "PIX_CAIXINHA"
            ? $"🐷 *Pix da Caixinha Registrado!*\n• Item: {parsed.Description}\n• Valor: R$ {parsed.Amount:F2}\n• Saldo Caixinha Atual: R$ {dashboardResult.Value?.NubankCaixinhaBalance:F2}\n• Teto restante: R$ {dashboardResult.Value?.RemainingBudget:F2}"
            : (parsed.Installments > 1
                ? $"💳 *Compra Parcelada Registrada!*\n• Item: {parsed.Description}\n• Total: R$ {parsed.Amount:F2} em {parsed.Installments}x de R$ {(parsed.Amount / parsed.Installments):F2}\n• Fatura atual: R$ {dashboardResult.Value?.CurrentInvoiceTotal:F2}"
                : $"💳 *Despesa Registrada!*\n• Item: {parsed.Description}\n• Valor: R$ {parsed.Amount:F2}\n• Categoria: {parsed.Category}\n• Fatura atual: R$ {dashboardResult.Value?.CurrentInvoiceTotal:F2}");

        return new CommandResponseModel
        {
            Success = true,
            Intent = "ADD_EXPENSE",
            Reply = reply,
            Dashboard = dashboardResult.Value,
            Data = result.Value
        };
    }

    private async Task<CommandResponseModel> HandleReceivePixAsync(ParsedExpenseIntentModel parsed)
    {
        var result = await _expenseService.ReceivePixAsync(new ReceivePixModel
        {
            Description = parsed.Description,
            Amount = parsed.Amount,
            Category = parsed.Category
        });

        if (!result.IsSuccess)
        {
            return new CommandResponseModel
            {
                Success = false,
                Intent = "RECEIVE_PIX",
                Reply = $"Erro ao registrar Pix recebido: {result.Error}"
            };
        }

        var dashboardResult = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "RECEIVE_PIX",
            Reply = $"💰 *Pix Externo Recebido!*\n• Descrição: {parsed.Description}\n• Valor: +R$ {parsed.Amount:F2}\n• Novo Saldo na Caixinha: R$ {dashboardResult.Value?.NubankCaixinhaBalance:F2} (115% CDI rendendo!)",
            Dashboard = dashboardResult.Value,
            Data = result.Value
        };
    }

    private async Task<CommandResponseModel> HandleDepositCaixinhaAsync(ParsedExpenseIntentModel parsed)
    {
        var result = await _profileService.DepositCaixinhaAsync(parsed.Amount);
        if (!result.IsSuccess || result.Value == null)
        {
            return new CommandResponseModel
            {
                Success = false,
                Intent = "DEPOSIT_CAIXINHA",
                Reply = $"Erro ao adicionar saldo na Caixinha: {result.Error}"
            };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "DEPOSIT_CAIXINHA",
            Reply = $"🐷 *Caixinha Abastecida!*\n• Adicionado: +R$ {parsed.Amount:F2}\n• Saldo atualizado: R$ {result.Value.Balance:F2}",
            Dashboard = dash.Value,
            Data = result.Value
        };
    }

    private async Task<CommandResponseModel> HandleStatusAsync()
    {
        var summaryResult = await _financialEngineService.GetDashboardSummaryAsync();
        if (!summaryResult.IsSuccess || summaryResult.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "STATUS", Reply = "Erro ao buscar resumo financeiro." };
        }

        var s = summaryResult.Value;
        var reply = $"📊 *Resumo Financeiro - Ciclo {s.BillingCycle}*\n\n" +
                    $"💳 *Cartão de Crédito:*\n" +
                    $"• Fatura atual: R$ {s.CurrentInvoiceTotal:F2}\n" +
                    $"• Teto do ciclo: R$ {s.CardHardCap:F2}\n" +
                    $"• Restante no teto: R$ {s.RemainingBudget:F2}\n" +
                    $"• Disponível por dia ({s.DaysUntilClosing}d restantes): R$ {s.AvailablePerDay:F2}/dia\n\n" +
                    $"🐷 *Nubank Caixinha (115% CDI):*\n" +
                    $"• Saldo investido: R$ {s.NubankCaixinhaBalance:F2}\n" +
                    $"• Rendimento diário estimado: R$ {s.NubankCaixinhaDailyYield:F2}/dia\n" +
                    $"• Rendimento mensal estimado: R$ {s.NubankCaixinhaMonthlyYield:F2}/mês\n\n" +
                    $"🎯 Meta de Aporte Mensal: R$ {s.MonthlyInvestmentGoal:F2}";

        return new CommandResponseModel
        {
            Success = true,
            Intent = "STATUS",
            Reply = reply,
            Dashboard = s,
            Data = s
        };
    }

    private async Task<CommandResponseModel> HandleSimulateAsync(ParsedExpenseIntentModel parsed)
    {
        var simResult = await _financialEngineService.SimulatePurchaseAsync(new SimulatorRequestModel
        {
            Amount = parsed.Amount,
            Installments = parsed.Installments
        });

        if (!simResult.IsSuccess || simResult.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "SIMULATE", Reply = "Erro ao processar simulação." };
        }

        var sim = simResult.Value;
        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        var reply = $"🔍 *Simulação de Compra*\n" +
                    $"• Valor: R$ {sim.Amount:F2} em {sim.Installments}x de R$ {sim.InstallmentValue:F2}\n" +
                    $"• Limite diário: de R$ {sim.CurrentDailyBefore:F2} para R$ {sim.CurrentDailyAfter:F2}/dia\n" +
                    $"• Veredito: {(sim.IsViable ? "✅ VIÁVEL" : "⚠️ ATENÇÃO")}\n" +
                    $"• {sim.Recommendation}";

        return new CommandResponseModel
        {
            Success = true,
            Intent = "SIMULATE",
            Reply = reply,
            Dashboard = dash.Value,
            Data = sim
        };
    }

    private async Task<CommandResponseModel> HandleUndoAsync()
    {
        var result = await _expenseService.DeleteLastExpenseAsync();
        if (!result.IsSuccess || result.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "UNDO", Reply = result.Error ?? "Nenhuma despesa recente para desfazer." };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "UNDO",
            Reply = $"🗑️ {result.Value.Message}\n• Fatura atual: R$ {dash.Value?.CurrentInvoiceTotal:F2}\n• Saldo Caixinha: R$ {dash.Value?.NubankCaixinhaBalance:F2}",
            Dashboard = dash.Value
        };
    }

    private async Task<CommandResponseModel> HandlePayInvoiceAsync()
    {
        var result = await _invoiceService.TogglePaymentAsync(null);
        if (!result.IsSuccess || result.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "PAY_INVOICE", Reply = result.Error ?? "Erro ao alterar status da fatura." };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "PAY_INVOICE",
            Reply = $"✅ {result.Value.Message}",
            Dashboard = dash.Value,
            Data = result.Value
        };
    }

    private async Task<CommandResponseModel> HandleInvestSurplusAsync()
    {
        var result = await _invoiceService.InvestSurplusAsync(null);
        if (!result.IsSuccess || result.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "INVEST_SURPLUS", Reply = result.Error ?? "Erro ao investir sobra da fatura." };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "INVEST_SURPLUS",
            Reply = $"🎉 {result.Value.Message}",
            Dashboard = dash.Value,
            Data = result.Value
        };
    }

    private async Task<CommandResponseModel> HandleSyncInvoiceAsync(ParsedExpenseIntentModel parsed)
    {
        var syncResult = await _expenseService.SyncInvoiceAsync(new SyncInvoiceModel { TargetTotal = parsed.Amount });
        if (!syncResult.IsSuccess || syncResult.Value == null)
        {
            return new CommandResponseModel { Success = false, Intent = "SYNC_INVOICE", Reply = syncResult.Error ?? "Erro ao sincronizar fatura." };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "SYNC_INVOICE",
            Reply = $"🔄 Fatura sincronizada com sucesso para R$ {parsed.Amount:F2}!",
            Dashboard = dash.Value,
            Data = syncResult.Value
        };
    }

    private async Task<CommandResponseModel> HandleUpdateCaixinhaAsync(ParsedExpenseIntentModel parsed)
    {
        var result = await _profileService.UpdateCaixinhaBalanceAsync(parsed.Amount);
        if (!result.IsSuccess)
        {
            return new CommandResponseModel { Success = false, Intent = "UPDATE_CAIXINHA", Reply = "Erro ao atualizar saldo da Caixinha." };
        }

        var dash = await _financialEngineService.GetDashboardSummaryAsync();
        return new CommandResponseModel
        {
            Success = true,
            Intent = "UPDATE_CAIXINHA",
            Reply = $"🐷 Saldo da Caixinha atualizado para R$ {parsed.Amount:F2}!",
            Dashboard = dash.Value
        };
    }
}
