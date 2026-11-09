using FinanceTracker.Application.Mappers;
using FinanceTracker.Application.Models;
using FinanceTracker.Application.Validations;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Results;
using Xunit;

namespace FinanceTracker.Tests;

public class MappersAndValidationTests
{
    [Fact]
    public void CardExpense_Mapper_ShouldMapCorrectly()
    {
        var entity = new CardExpenseEntity
        {
            Id = 42,
            Uuid = Guid.NewGuid(),
            Description = "Almoço",
            Category = "Alimentação",
            Amount = 45.50m,
            TransactionDate = new DateTime(2026, 11, 8),
            BillingCycle = "2026-12",
            InstallmentCurrent = 1,
            InstallmentTotal = 1,
            PaymentMethod = "CARD",
            DeductFromCycleCap = true
        };

        var model = entity.ToModel();

        Assert.Equal(entity.Id, model.Id);
        Assert.Equal(entity.Uuid, model.Uuid);
        Assert.Equal(entity.Description, model.Description);
        Assert.Equal(entity.Amount, model.Amount);
        Assert.Equal(entity.BillingCycle, model.BillingCycle);
        Assert.Equal(entity.PaymentMethod, model.PaymentMethod);
    }

    [Fact]
    public void CreateExpenseValidator_ShouldFail_WhenAmountZeroOrNegative()
    {
        var validator = new CreateExpenseModelValidator();
        var model = new CreateExpenseModel
        {
            Description = "Teste",
            Amount = -10m,
            Installments = 1
        };

        var result = validator.Validate(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void CreateExpenseValidator_ShouldSucceed_WhenValid()
    {
        var validator = new CreateExpenseModelValidator();
        var model = new CreateExpenseModel
        {
            Description = "Mercado",
            Amount = 150m,
            Installments = 1
        };

        var result = validator.Validate(model);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Result_ImplicitConversions_ShouldWork()
    {
        Result<string> success = "dados ok";
        Assert.True(success.IsSuccess);
        Assert.Equal("dados ok", success.Value);

        Result<string> failure = ErrorMessages.EntityNotFound;
        Assert.False(failure.IsSuccess);
        Assert.Equal("Registro não encontrado.", failure.Error);
        Assert.Equal(404, failure.StatusCode);
    }
}
