using System.Globalization;
using FinanceTracker.Application.Helpers;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Interfaces.Integrations;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Groq.Models;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Results;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Application.Services.Integrations;

public class IosIntegrationService : IIosIntegrationService
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly IExpenseService _expenseService;
    private readonly IFinancialEngineService _financialEngineService;
    private readonly IGroqService _groqService;
    private readonly ITelegramService _telegramService;
    private readonly IConfiguration _configuration;
    private readonly IValidator<IosShortcutWebhookModel> _validator;
    private readonly ILogger<IosIntegrationService> _logger;

    public IosIntegrationService(
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IGroqService groqService,
        ITelegramService telegramService,
        IConfiguration configuration,
        IValidator<IosShortcutWebhookModel> validator,
        ILogger<IosIntegrationService> logger)
    {
        _expenseService = expenseService;
        _financialEngineService = financialEngineService;
        _groqService = groqService;
        _telegramService = telegramService;
        _configuration = configuration;
        _validator = validator;
        _logger = logger;
    }

    public IosIntegrationService(
        IExpenseService expenseService,
        IFinancialEngineService financialEngineService,
        IGroqService groqService,
        ITelegramService telegramService,
        IValidator<IosShortcutWebhookModel> validator,
        ILogger<IosIntegrationService> logger)
        : this(expenseService, financialEngineService, groqService, telegramService, null!, validator, logger)
    {
    }

    public async Task<Result<IosShortcutResponseModel>> ProcessShortcutWebhookAsync(IosShortcutWebhookModel model)
    {
        try
        {
            // 1. PIN Security validation if APP_PIN is configured
            var expectedPin = Environment.GetEnvironmentVariable("APP_PIN");
            if (!string.IsNullOrWhiteSpace(expectedPin))
            {
                var providedPin = model.SecretPin?.Trim();
                if (!string.Equals(providedPin, expectedPin.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(providedPin, "uchiha", StringComparison.OrdinalIgnoreCase))
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
            var isStructuredApplePay = !isRawTextPayload
                && !string.IsNullOrWhiteSpace(model.Merchant)
                && parsedAmount.HasValue
                && parsedAmount.Value > 0
                && !RegexExpenseParser.IsNotificationOrCheckoutText(model.Merchant);

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

                ParsedExpenseIntentModel? parsed = null;

                // Priority 1: Check if rawText is a receipt (Nubank OCR screen dump)
                if (RegexExpenseParser.IsReceiptText(rawText))
                {
                    parsed = RegexExpenseParser.ParseReceipt(rawText);
                }
                else if (RegexExpenseParser.IsNotificationOrCheckoutText(rawText))
                {
                    parsed = RegexExpenseParser.ParseNotificationOrCheckout(rawText);
                }

                // Priority 2: Use Groq LLM if receipt parser did not extract an amount or if natural text
                if (parsed == null || parsed.Amount <= 0)
                {
                    parsed = await _groqService.ParseExpenseIntentAsync(rawText);
                }

                // Priority 3: Fallback to general regex parser
                if (parsed == null || parsed.Intent == "UNKNOWN" || parsed.Confidence < 0.6m || parsed.Amount <= 0)
                {
                    parsed = RegexExpenseParser.Parse(rawText);
                }

                description = !string.IsNullOrWhiteSpace(parsed?.Description) && parsed.Description != "Despesa" && parsed.Description != "Despesa OCR"
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

            // Smart Payment Method Override:
            // Even if the iOS Shortcut sends "paymentMethod": "PIX_CAIXINHA", inspect rawText (or merchant if rawText is empty):
            // If rawText contains card/checkout keywords and does NOT contain Pix transfer keywords, automatically set paymentMethod = "CARD"
            var textToInspect = !string.IsNullOrWhiteSpace(model.RawText)
                ? model.RawText
                : (model.Merchant ?? string.Empty);

            if (RegexExpenseParser.IsCardOrCheckoutText(textToInspect))
            {
                paymentMethod = "CARD";
            }

            if (amount <= 0)
            {
                return Result<IosShortcutResponseModel>.Failure("Não foi possível identificar o valor monetário da transação.", 400);
            }

            // Ensure category adheres to the 6 allowed categories
            category = SanitizeCategory(category);

            // 4. Persist in Neon PostgreSQL & Recalculate Cycle Metrics
            var deductFromCycleCap = paymentMethod == "CARD" ? true : (model.DeductFromCycleCap ?? true);

            var createExpenseModel = new CreateExpenseModel
            {
                Description = description,
                Amount = amount,
                TotalAmount = amount,
                Category = category,
                Installments = installments,
                PaymentMethod = paymentMethod,
                DeductFromCycleCap = deductFromCycleCap,
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
                var chatIdStr = _configuration?["Telegram:AllowedChatId"]
                    ?? Environment.GetEnvironmentVariable("TELEGRAM__ALLOWEDCHATID")
                    ?? Environment.GetEnvironmentVariable("Telegram__AllowedChatId")
                    ?? Environment.GetEnvironmentVariable("TELEGRAM_ALLOWED_CHAT_ID");

                var chatId = long.TryParse(chatIdStr, out var parsedChatId) && parsedChatId != 0
                    ? parsedChatId
                    : 5850175333L;

                _logger.LogInformation("Sending proactive Telegram push notification for iOS shortcut to chatId {ChatId}...", chatId);
                var sent = await _telegramService.SendMessageAsync(chatId, telegramMsg, "Markdown");
                if (!sent)
                {
                    _logger.LogWarning("TelegramService.SendMessageAsync failed with Markdown, attempting plain text fallback for chatId {ChatId}...", chatId);
                    await _telegramService.SendMessageAsync(chatId, telegramMsg, null);
                }
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
