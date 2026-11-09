using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Models;
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
        public Task<Result<DashboardSummaryModel>> GetDashboardSummaryAsync(string? cycle = null)
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
}
