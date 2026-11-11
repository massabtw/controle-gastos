using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Application.Services.Integrations;
using FinanceTracker.Application.Validations;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Groq.Models;
using FinanceTracker.Domain.Http.Telegram;
using FinanceTracker.Domain.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceTracker.Tests;

[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollectionDefinition { }

[Collection("Sequential")]
public class IosIntegrationServiceTests
{
    private class FakeExpenseService : IExpenseService
    {
        public CreateExpenseModel? LastCreatedModel { get; private set; }

        public Task<Result<IEnumerable<ExpenseItemModel>>> CreateExpenseAsync(CreateExpenseModel model)
        {
            LastCreatedModel = model;
            var item = new ExpenseItemModel
            {
                Id = 1,
                Description = model.Description,
                Amount = model.Amount,
                Category = model.Category,
                PaymentMethod = model.PaymentMethod ?? "CARD",
                InstallmentCurrent = 1,
                InstallmentTotal = model.Installments
            };
            return Task.FromResult(Result<IEnumerable<ExpenseItemModel>>.Success(new List<ExpenseItemModel> { item }));
        }

        public Task<Result<ExpenseItemModel>> CreatePixCaixinhaExpenseAsync(CreateExpenseModel model) => throw new NotImplementedException();
        public Task<Result<MessageResponseModel>> DeleteExpenseAsync(int id, bool deleteAllInstallments = false) => throw new NotImplementedException();
        public Task<Result<MessageResponseModel>> DeleteLastExpenseAsync() => throw new NotImplementedException();
        public Task<Result<ExpenseItemModel>> GetExpenseByIdAsync(int id) => throw new NotImplementedException();
        public Task<Result<IEnumerable<ExpenseItemModel>>> GetExpensesByCycleAsync(string? cycle = null) => throw new NotImplementedException();
        public Task<Result<ExpenseItemModel>> ReceivePixAsync(ReceivePixModel model) => throw new NotImplementedException();
        public Task<Result<SyncInvoiceResultModel>> SyncInvoiceAsync(SyncInvoiceModel model) => throw new NotImplementedException();
        public Task<Result<MessageResponseModel>> UpdateExpenseAsync(int id, UpdateExpenseModel model) => throw new NotImplementedException();
    }

    private class FakeFinancialEngineService : IFinancialEngineService
    {
        public Task<Result<DashboardSummaryModel>> GetDashboardSummaryAsync(string? cycle = null, DateTime? referenceDate = null)
        {
            return Task.FromResult(Result<DashboardSummaryModel>.Success(new DashboardSummaryModel
            {
                CurrentInvoiceTotal = 406.00m,
                CardHardCap = 1030.00m,
                RemainingBudget = 624.00m,
                AvailablePerDay = 31.20m,
                DaysUntilClosing = 20
            }));
        }

        public Task<Result<SimulatorResultModel>> SimulatePurchaseAsync(SimulatorRequestModel request) => throw new NotImplementedException();
        public Task<Result<IEnumerable<FutureCycleProjectionModel>>> GetProjectionsAsync(int months = 6) => throw new NotImplementedException();
    }

    private class FakeGroqService : IGroqService
    {
        public Task<ParsedExpenseIntentModel?> ParseExpenseIntentAsync(string userMessage)
        {
            if (userMessage.Contains("Barbearia Silva"))
            {
                return Task.FromResult<ParsedExpenseIntentModel?>(new ParsedExpenseIntentModel
                {
                    Intent = "ADD_EXPENSE",
                    Description = "Barbearia Silva",
                    Amount = 35.00m,
                    Category = "Compras",
                    PaymentMethod = "PIX_CAIXINHA",
                    Installments = 1,
                    Confidence = 0.95m
                });
            }

            return Task.FromResult<ParsedExpenseIntentModel?>(null);
        }
    }

    private class FakeTelegramService : ITelegramService
    {
        public string? LastMessage { get; private set; }
        public long LastChatId { get; private set; }

        public Task<bool> SendMessageAsync(long chatId, string text, string? parseMode = "Markdown")
        {
            LastChatId = chatId;
            LastMessage = text;
            return Task.FromResult(true);
        }

        public Task<bool> SendToAllowedChatAsync(string text, string? parseMode = "Markdown")
        {
            LastMessage = text;
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_StructuredApplePay_ShouldCreateExpenseAndPushTelegram()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            Merchant = "Posto Ipiranga",
            Amount = "R$ 50,00",
            PaymentMethod = "CARD"
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(result.Value);
        Assert.Equal(31.20m, result.Value.DailyBurnRate);
        Assert.Equal(624.00m, result.Value.RemainingCap);
        Assert.Contains("Posto Ipiranga", result.Value.Message);
        Assert.Contains("R$ 50,00", result.Value.Message);
        Assert.Contains("R$ 31,20/dia", result.Value.Message);

        Assert.NotNull(expenseService.LastCreatedModel);
        Assert.Equal("Posto Ipiranga", expenseService.LastCreatedModel.Description);
        Assert.Equal(50.00m, expenseService.LastCreatedModel.Amount);
        Assert.Equal("Transporte", expenseService.LastCreatedModel.Category);
        Assert.Equal("CARD", expenseService.LastCreatedModel.PaymentMethod);

        Assert.NotNull(telegram.LastMessage);
        Assert.Contains("🍏 *iPhone Auto-Sync (Apple Pay)*", telegram.LastMessage);
        Assert.Contains("Posto Ipiranga", telegram.LastMessage);
        Assert.Contains("R$ 50,00", telegram.LastMessage);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_RawOcrNubankPix_ShouldParseAndSetPixCaixinha()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "Comprovante de transferência Pix R$ 35,00 para Barbearia Silva",
            PaymentMethod = "PIX_CAIXINHA"
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(result.Value);
        Assert.NotNull(expenseService.LastCreatedModel);
        Assert.Equal("Barbearia Silva", expenseService.LastCreatedModel.Description);
        Assert.Equal(35.00m, expenseService.LastCreatedModel.Amount);
        Assert.Equal("PIX_CAIXINHA", expenseService.LastCreatedModel.PaymentMethod);

        Assert.NotNull(telegram.LastMessage);
        Assert.Contains("🍏 *iPhone Auto-Sync (Pix Caixinha)*", telegram.LastMessage);
        Assert.Contains("R$ 35,00", telegram.LastMessage);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_RawOcrWithExplicitInstallments_ShouldKeepExplicitInstallments()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "Comprovante de transferência Pix R$ 35,00 para Barbearia Silva",
            Installments = 3
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(expenseService.LastCreatedModel);
        Assert.Equal(3, expenseService.LastCreatedModel.Installments);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_InvalidPin_ShouldReturn401()
    {
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        Environment.SetEnvironmentVariable("APP_PIN", "1234");
        try
        {
            var request = new IosShortcutWebhookModel
            {
                Merchant = "McDonald's",
                Amount = 42.50m,
                SecretPin = "0000"
            };

            var result = await service.ProcessShortcutWebhookAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APP_PIN", null);
        }
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_RawOcrNubankPixDetailedScreenDump_ShouldParseAmountAndRecipientAndDeductFromCap()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService(); // returns null, forcing regex OCR fallback
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "Comprovante de transferência 09 NOV 2026 - 16:14:50 Valor R$ 0,01 Tipo de transferência Pix Destino Nome Gustavo Antonio A Rodrigues CPF ***.123.456-**",
            PaymentMethod = "PIX_CAIXINHA"
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(result.Value);
        Assert.NotNull(expenseService.LastCreatedModel);

        // 1. Amount should be 0.01 (NOT 9 from date 09 NOV 2026)
        Assert.Equal(0.01m, expenseService.LastCreatedModel.Amount);

        // 2. Description should be Gustavo Antonio A Rodrigues
        Assert.Equal("Gustavo Antonio A Rodrigues", expenseService.LastCreatedModel.Description);

        // 3. PIX_CAIXINHA should deduct from monthly spending cap by default
        Assert.Equal("PIX_CAIXINHA", expenseService.LastCreatedModel.PaymentMethod);
        Assert.True(expenseService.LastCreatedModel.DeductFromCycleCap);

        // 4. Proactive Telegram push must be delivered
        Assert.NotNull(telegram.LastMessage);
        Assert.Contains("🍏 *iPhone Auto-Sync (Pix Caixinha)*", telegram.LastMessage);
        Assert.Contains("Gustavo Antonio A Rodrigues", telegram.LastMessage);
        Assert.Contains("R$ 0,01", telegram.LastMessage);
        Assert.Equal(5850175333L, telegram.LastChatId);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_EmptyPayload_ShouldReturn400()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            Merchant = null,
            RawText = null
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Merchant", result.Error);
    }

    [Theory]
    [InlineData("Comprovante de transferência 09 NOV 2026 - 16:14:50 Valor R$ 0,01 Tipo de transferência Pix Destino Nome Gustavo Antonio A Rodrigues", 0.01, "Gustavo Antonio A Rodrigues")]
    [InlineData("Comprovante de transferência Pix R$ 35,00 para Barbearia Silva", 35.00, "Barbearia Silva")]
    [InlineData("Comprovante Pix\nValor: R$ 1.250,50\nDestino Nome: Loja de Moveis LTDA CNPJ: 12.345.678/0001-90", 1250.50, "Loja de Moveis LTDA")]
    public void RegexExpenseParser_Parse_NubankReceipts_ShouldExtractCorrectAmountAndRecipient(string receiptText, decimal expectedAmount, string expectedRecipient)
    {
        var result = RegexExpenseParser.Parse(receiptText);

        Assert.Equal("ADD_EXPENSE", result.Intent);
        Assert.Equal(expectedAmount, result.Amount);
        Assert.Equal(expectedRecipient, result.Description);
        Assert.Equal("PIX_CAIXINHA", result.PaymentMethod);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_NotificationBanner_ShouldOverrideToCardAndExtractMerchant()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService(); // null fallback to regex
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "Compra de R$ 45,90 aprovada em iFood no seu cartão",
            PaymentMethod = "PIX_CAIXINHA" // Shortcut mistakenly sent PIX_CAIXINHA
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(result.Value);
        Assert.NotNull(expenseService.LastCreatedModel);

        Assert.Equal("iFood", expenseService.LastCreatedModel.Description);
        Assert.Equal(45.90m, expenseService.LastCreatedModel.Amount);
        Assert.Equal("Alimentação", expenseService.LastCreatedModel.Category);
        Assert.Equal("CARD", expenseService.LastCreatedModel.PaymentMethod);
        Assert.True(expenseService.LastCreatedModel.DeductFromCycleCap);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_InAppCheckoutScreen_ShouldOverrideToCard()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "iFood - Pedido #1234: Total R$ 68,50 pago com Apple Pay",
            PaymentMethod = "PIX_CAIXINHA"
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(result.Value);
        Assert.NotNull(expenseService.LastCreatedModel);

        Assert.Equal("iFood", expenseService.LastCreatedModel.Description);
        Assert.Equal(68.50m, expenseService.LastCreatedModel.Amount);
        Assert.Equal("CARD", expenseService.LastCreatedModel.PaymentMethod);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_NotificationWithInstallments_ShouldExtractInstallmentsAndOverrideToCard()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "Compra de R$ 150,00 em 3x aprovada em Magazine Luiza",
            PaymentMethod = "PIX_CAIXINHA"
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(expenseService.LastCreatedModel);

        Assert.Equal("Magazine Luiza", expenseService.LastCreatedModel.Description);
        Assert.Equal(150.00m, expenseService.LastCreatedModel.Amount);
        Assert.Equal(3, expenseService.LastCreatedModel.Installments);
        Assert.Equal("Compras", expenseService.LastCreatedModel.Category);
        Assert.Equal("CARD", expenseService.LastCreatedModel.PaymentMethod);
    }

    [Theory]
    [InlineData("Compra aprovada no seu cartão em Uber de R$ 22,40", true)]
    [InlineData("Compra de R$ 89,90 aprovada em Mercado Livre", true)]
    [InlineData("Amazon - Pedido finalizado R$ 129,00 em 2x", true)]
    [InlineData("Pagamento de fatura de cartão R$ 450,00", true)]
    [InlineData("Comprovante de transferência 09 NOV 2026 Valor R$ 50,00 Tipo de transferência Pix", false)]
    [InlineData("Pix enviado com sucesso R$ 30,00 para João", false)]
    [InlineData("Comprovante Pix R$ 25,00 para Uber Motorista", false)]
    [InlineData("paguei a fatura R$ 450", false)]
    [InlineData("simula R$ 1000 em 10x", false)]
    public void RegexExpenseParser_IsCardOrCheckoutText_ShouldCorrectlyDifferentiate(string input, bool expected)
    {
        var isCard = RegexExpenseParser.IsCardOrCheckoutText(input);
        Assert.Equal(expected, isCard);
    }

    [Theory]
    [InlineData("paguei a fatura R$ 450", "PAY_INVOICE")]
    [InlineData("simula R$ 1000 em 10x", "SIMULATE")]
    [InlineData("ajustar fatura R$ 850,00", "SYNC_INVOICE")]
    [InlineData("recebi R$ 120 de reembolso", "RECEIVE_PIX")]
    public void RegexExpenseParser_Parse_ChatCommandsWithCardKeywords_ShouldNotBeHijacked(string command, string expectedIntent)
    {
        var result = RegexExpenseParser.Parse(command);
        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public void RegexExpenseParser_ParseNotificationOrCheckout_QuantityAndDeliveryTimeAndIntegerAmount_ShouldParseCleanly()
    {
        var checkoutWithQuantity = RegexExpenseParser.ParseNotificationOrCheckout("iFood - Pedido entregue em 30 min: 2x Hambúrguer Artesanal Total R$ 150");
        Assert.NotNull(checkoutWithQuantity);
        Assert.Equal("iFood", checkoutWithQuantity.Description);
        Assert.Equal(150.00m, checkoutWithQuantity.Amount);
        Assert.Equal(1, checkoutWithQuantity.Installments); // "2x Hambúrguer" is item quantity, NOT 2 installments
        Assert.Equal("CARD", checkoutWithQuantity.PaymentMethod);
    }

    [Fact]
    public async Task ProcessShortcutWebhookAsync_ExplicitPixAccountOrDeductFalse_ShouldRespectCallerSettings()
    {
        Environment.SetEnvironmentVariable("APP_PIN", null);
        var expenseService = new FakeExpenseService();
        var financialEngine = new FakeFinancialEngineService();
        var groq = new FakeGroqService();
        var telegram = new FakeTelegramService();
        var validator = new IosShortcutWebhookModelValidator();

        var service = new IosIntegrationService(
            expenseService,
            financialEngine,
            groq,
            telegram,
            validator,
            NullLogger<IosIntegrationService>.Instance
        );

        var request = new IosShortcutWebhookModel
        {
            RawText = "iFood - Pedido #999 Total R$ 40,00",
            PaymentMethod = "PIX_ACCOUNT",
            DeductFromCycleCap = false
        };

        var result = await service.ProcessShortcutWebhookAsync(request);

        Assert.True(result.IsSuccess, result.Error ?? "Error was null");
        Assert.NotNull(expenseService.LastCreatedModel);
        Assert.Equal("PIX_ACCOUNT", expenseService.LastCreatedModel.PaymentMethod);
        Assert.False(expenseService.LastCreatedModel.DeductFromCycleCap);
    }
}
