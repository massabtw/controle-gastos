using System.Globalization;
using FinanceTracker.Application.Helpers;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Interfaces.Integrations;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Integrations;

public class IosIntegrationService : IIosIntegrationService
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly IExpenseService _expenseService;
    private readonly IFinancialEngineService _financialEngineService;
    private readonly IGroqService _groqService;
    private readonly ITelegramService _telegramService;
    private readonly IValidator<IosShortcutWebhookModel> _validator;
    private readonly ILogger<IosIntegrationService> _logger;

    public IosIntegrationService(
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IGroqService groqService,
        ITelegramService telegramService,
        IValidator<IosShortcutWebhookModel> validator,
        ILogger<IosIntegrationService> logger)
    {
        _expenseService = expenseService;
        _financialEngineService = financialEngineService;
        _groqService = groqService;
        _telegramService = telegramService;
        _validator = validator;
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

            // 2. Validate input model with FluentValidation
            var validationResult = await _validator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return Result<IosShortcutResponseModel>.Failure(validationResult.Errors[0].ErrorMessage, 400);
            }

            // 3. Parse currency amount resiliently
            var parsedAmount = CurrencyParser.Parse(model.Amount);

            string description;
            decimal amount;
            string category;
            string paymentMethod;
            int installments;
            DateTime? transactionDate = null;

            var isRawTextPayload = !string.IsNullOrWhiteSpace(model.RawText);
            var isStructuredApplePay = !isRawTextPayload && !string.IsNullOrWhiteSpace(model.Merchant) && parsedAmount.HasValue && parsedAmount.Value > 0;

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
                    if (nlpParsed != null && !string.IsNullOrWhiteSpace(nlpParsed.Category))
                    {
                        category = nlpParsed.Category;
                    }
                }
            }
            else
            {
                // Case B — Raw OCR / Natural Text Payload (RawText present, or Amount is null/0)
                var rawText = !string.IsNullOrWhiteSpace(model.RawText)
                    ? model.RawText.Trim()
                    : (model.Merchant?.Trim() ?? string.Empty);

                var parsed = await _groqService.ParseExpenseIntentAsync(rawText);
                if (parsed == null || parsed.Intent == "UNKNOWN" || parsed.Confidence < 0.6m)
                {
                    parsed = RegexExpenseParser.Parse(rawText);
                }

                description = !string.IsNullOrWhiteSpace(parsed?.Description) && parsed.Description != "Despesa"
                    ? parsed.Description
                    : (!string.IsNullOrWhiteSpace(model.Merchant) ? model.Merchant.Trim() : "Despesa iOS");

                amount = (parsed?.Amount > 0) ? parsed.Amount : (parsedAmount ?? 0);
                category = !string.IsNullOrWhiteSpace(parsed?.Category)
                    ? parsed.Category
                    : RegexExpenseParser.InferCategory(description);

                paymentMethod = !string.IsNullOrWhiteSpace(parsed?.PaymentMethod)
                    ? parsed.PaymentMethod.ToUpperInvariant()
                    : "CARD";

                // Explicit installments passed in request takes precedence over parsed
                installments = model.Installments is > 0 ? model.Installments.Value : (parsed?.Installments > 0 ? parsed.Installments : 1);

                transactionDate = parsed?.TransactionDate;

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

            // Ensure category adheres to the 6 allowed categories
            category = SanitizeCategory(category);

            // 4. Persist in Neon PostgreSQL & Recalculate Cycle Metrics
            var createExpenseModel = new CreateExpenseModel
            {
                Description = description,
                Amount = amount,
                TotalAmount = amount,
                Category = category,
                Installments = installments,
                PaymentMethod = paymentMethod,
                DeductFromCycleCap = true,
                TransactionDate = transactionDate ?? DateTime.Now
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

            // 5. Proactive Telegram Push Notification
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
                $"💸 Valor: *R$ {amount.ToString("F2", PtBrCulture)}*\n" +
                $"📊 Fatura / Ciclo: *R$ {currentInvoiceTotal.ToString("F2", PtBrCulture)}* / R$ {hardCap.ToString("F2", PtBrCulture)}\n" +
                $"🔥 Novo Teto Diário: *R$ {dailyBurnRate.ToString("F2", PtBrCulture)}/dia* ({daysRemaining}d restantes)";

            try
            {
                await _telegramService.SendToAllowedChatAsync(telegramMsg, "Markdown");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Telegram notification");
            }

            // 6. Endpoint Response for iOS Banner Feedback
            var responseMessage = $"✅ R$ {amount.ToString("F2", PtBrCulture)} em {description} registrado! Novo teto: R$ {dailyBurnRate.ToString("F2", PtBrCulture)}/dia";

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

    private static string SanitizeCategory(string? category)
    {
        return category switch
        {
            "Alimentação" or "Transporte" or "Lazer" or "Compras" or "Saúde" or "Outros" => category,
            _ => "Outros"
        };
    }
}
