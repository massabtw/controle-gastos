using FinanceTracker.Application.Helpers;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Interfaces.Integrations;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Results;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Integrations;

public class IosIntegrationService : IIosIntegrationService
{
    private readonly IExpenseService _expenseService;
    private readonly IFinancialEngineService _financialEngineService;
    private readonly IGroqService _groqService;
    private readonly ITelegramService _telegramService;
    private readonly ILogger<IosIntegrationService> _logger;

    public IosIntegrationService(
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IGroqService groqService,
        ITelegramService telegramService,
        ILogger<IosIntegrationService> logger)
    {
        _expenseService = expenseService;
        _financialEngineService = financialEngineService;
        _groqService = groqService;
        _telegramService = telegramService;
        _logger = logger;
    }

    public async Task<Result<IosShortcutResponseModel>> ProcessShortcutWebhookAsync(IosShortcutWebhookModel model)
    {
        try
        {
            // 1. PIN Security validation if APP_PIN is configured
            var expectedPin = Environment.GetEnvironmentVariable("APP_PIN");
            if (!string.IsNullOrWhiteSpace(expectedPin))
            {
                if (!string.Equals(model.SecretPin?.Trim(), expectedPin.Trim(), StringComparison.Ordinal))
                {
                    return Result<IosShortcutResponseModel>.Failure("PIN de acesso inválido ou não informado.", 401);
                }
            }

            // 2. Parse currency amount resiliently
            var parsedAmount = CurrencyParser.Parse(model.Amount);

            string description;
            decimal amount;
            string category;
            string paymentMethod;
            int installments;

            var isStructuredApplePay = !string.IsNullOrWhiteSpace(model.Merchant) && parsedAmount.HasValue && parsedAmount.Value > 0;

            if (isStructuredApplePay)
            {
                // Case A — Structured Apple Pay Payload
                description = model.Merchant!.Trim();
                amount = parsedAmount!.Value;
                paymentMethod = !string.IsNullOrWhiteSpace(model.PaymentMethod) ? model.PaymentMethod.Trim().ToUpperInvariant() : "CARD";
                installments = model.Installments is > 0 ? model.Installments.Value : 1;
                category = RegexExpenseParser.InferCategory(description);

                if (category == "Outros")
                {
                    var nlpParsed = await _groqService.ParseExpenseIntentAsync(description);
                    if (nlpParsed != null && !string.IsNullOrWhiteSpace(nlpParsed.Category) && nlpParsed.Category != "Outros")
                    {
                        category = nlpParsed.Category;
                    }
                }
            }
            else
            {
                // Case B — Raw OCR / Natural Text Payload
                var rawText = !string.IsNullOrWhiteSpace(model.RawText)
                    ? model.RawText.Trim()
                    : (model.Merchant?.Trim() ?? string.Empty);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    return Result<IosShortcutResponseModel>.Failure("Nenhum dado de transação, estabelecimento ou texto OCR foi fornecido.", 400);
                }

                var parsed = await _groqService.ParseExpenseIntentAsync(rawText);
                if (parsed == null || parsed.Intent == "UNKNOWN" || parsed.Confidence < 0.6m)
                {
                    parsed = RegexExpenseParser.Parse(rawText);
                }

                description = !string.IsNullOrWhiteSpace(parsed?.Description) && parsed.Description != "Despesa"
                    ? parsed.Description
                    : (!string.IsNullOrWhiteSpace(model.Merchant) ? model.Merchant.Trim() : "Despesa iOS");

                amount = (parsed?.Amount > 0) ? parsed.Amount : (parsedAmount ?? 0);
                category = !string.IsNullOrWhiteSpace(parsed?.Category) && parsed.Category != "Outros"
                    ? parsed.Category
                    : RegexExpenseParser.InferCategory(description);

                paymentMethod = !string.IsNullOrWhiteSpace(parsed?.PaymentMethod)
                    ? parsed.PaymentMethod.ToUpperInvariant()
                    : "CARD";

                installments = parsed?.Installments ?? (model.Installments is > 0 ? model.Installments.Value : 1);

                // Explicit PaymentMethod override if passed in request
                if (!string.IsNullOrWhiteSpace(model.PaymentMethod))
                {
                    paymentMethod = model.PaymentMethod.Trim().ToUpperInvariant();
                }
            }

            if (amount <= 0)
            {
                return Result<IosShortcutResponseModel>.Failure("Não foi possível identificar o valor monetário da transação.", 400);
            }

            // 3. Persist in Neon PostgreSQL & Recalculate Cycle Metrics
            var createExpenseModel = new CreateExpenseModel
            {
                Description = description,
                Amount = amount,
                TotalAmount = amount,
                Category = category,
                Installments = installments,
                PaymentMethod = paymentMethod,
                DeductFromCycleCap = true,
                TransactionDate = DateTime.Now
            };

            var expenseResult = await _expenseService.CreateExpenseAsync(createExpenseModel);
            if (!expenseResult.IsSuccess)
            {
                return Result<IosShortcutResponseModel>.Failure(expenseResult.Error ?? "Erro ao registrar a despesa.", expenseResult.StatusCode);
            }

            var createdExpense = expenseResult.Value?.FirstOrDefault();

            // Fetch updated dashboard summary for cycle metrics
            var dashboardResult = await _financialEngineService.GetDashboardSummaryAsync();
            var summary = dashboardResult.Value;

            var currentInvoiceTotal = summary?.CurrentInvoiceTotal ?? 0;
            var hardCap = summary?.CardHardCap ?? 0;
            var remainingCap = summary?.RemainingBudget ?? 0;
            var dailyBurnRate = summary?.AvailablePerDay ?? 0;
            var daysRemaining = summary?.DaysUntilClosing ?? 0;

            // 4. Proactive Telegram Push Notification
            var paymentMethodLabel = paymentMethod switch
            {
                "PIX_CAIXINHA" => "Pix Caixinha",
                "PIX_ACCOUNT" => "Pix Conta",
                "CARD" => "Apple Pay",
                _ => paymentMethod
            };

            var telegramMsg =
                $"🍏 *iPhone Auto-Sync ({paymentMethodLabel})*\n" +
                $"✅ Lançado: *{description}* ({category})\n" +
                $"💸 Valor: *R$ {amount:F2}*\n" +
                $"📊 Fatura / Ciclo: *R$ {currentInvoiceTotal:F2}* / R$ {hardCap:F2}\n" +
                $"🔥 Novo Teto Diário: *R$ {dailyBurnRate:F2}/dia* ({daysRemaining}d restantes)";

            try
            {
                await _telegramService.SendToAllowedChatAsync(telegramMsg, "Markdown");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Telegram notification");
            }

            // 5. Endpoint Response for iOS Banner Feedback
            var responseMessage = $"✅ R$ {amount:F2} em {description} registrado! Novo teto: R$ {dailyBurnRate:F2}/dia";

            var responseModel = new IosShortcutResponseModel
            {
                Success = true,
                Message = responseMessage,
                Expense = createdExpense,
                DailyBurnRate = dailyBurnRate,
                RemainingCap = remainingCap
            };

            return Result<IosShortcutResponseModel>.Success(responseModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling iOS Shortcut webhook");
            return Result<IosShortcutResponseModel>.Failure("Erro interno ao processar o webhook do iOS.", 500);
        }
    }
}
