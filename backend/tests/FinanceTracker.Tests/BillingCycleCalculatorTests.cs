using FinanceTracker.Application.Services.Ai;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Services;
using Xunit;

namespace FinanceTracker.Tests;

public class BillingCycleCalculatorTests
{
    [Theory]
    [InlineData("2026-11-07", 8, "2026-11")]
    [InlineData("2026-11-08", 8, "2026-12")]
    [InlineData("2026-11-09", 8, "2026-12")]
    [InlineData("2026-12-07", 8, "2026-12")]
    [InlineData("2026-12-08", 8, "2027-01")]
    public void GetTargetBillingCycle_ShouldResolveCorrectly(string dateStr, int closingDay, string expectedCycle)
    {
        var date = DateTime.Parse(dateStr);
        var cycle = BillingCycleCalculator.GetTargetBillingCycle(date, closingDay);
        Assert.Equal(expectedCycle, cycle);
    }

    [Fact]
    public void GetCommitmentInstallmentStatus_RecurringFixed_ShouldBeFixo()
    {
        var commitment = new FixedCommitmentEntity
        {
            Title = "Internet Móvel Mãe",
            Amount = 50.00m,
            StartCycle = "2026-12",
            TotalInstallments = null,
            IsActive = true
        };

        Assert.Null(BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2026-11"));
        Assert.Equal("Fixo", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2026-12"));
        Assert.Equal("Fixo", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-05"));
    }

    [Fact]
    public void GetCommitmentInstallmentStatus_FixedInstallments_ShouldTrackAndExpire()
    {
        var commitment = new FixedCommitmentEntity
        {
            Title = "Monitor Mãe",
            Amount = 120.00m,
            StartCycle = "2026-12",
            TotalInstallments = 5,
            IsActive = true
        };

        Assert.Null(BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2026-11"));
        Assert.Equal("1/5", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2026-12"));
        Assert.Equal("2/5", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-01"));
        Assert.Equal("3/5", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-02"));
        Assert.Equal("4/5", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-03"));
        Assert.Equal("5/5", BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-04"));
        // Expired after 5 months
        Assert.Null(BillingCycleCalculator.GetCommitmentInstallmentStatus(commitment, "2027-05"));
    }

    [Fact]
    public void RegexExpenseParser_ShouldParseExpensesCorrectly()
    {
        var result1 = RegexExpenseParser.Parse("uber 25");
        Assert.Equal("ADD_EXPENSE", result1.Intent);
        Assert.Equal("Uber", result1.Description);
        Assert.Equal(25.00m, result1.Amount);
        Assert.Equal(1, result1.Installments);
        Assert.Equal("Transporte", result1.Category);

        var result2 = RegexExpenseParser.Parse("comprei um teclado 300 em 3x");
        Assert.Equal("ADD_EXPENSE", result2.Intent);
        Assert.Equal("Teclado", result2.Description);
        Assert.Equal(300.00m, result2.Amount);
        Assert.Equal(3, result2.Installments);

        var result3 = RegexExpenseParser.Parse("/status");
        Assert.Equal("STATUS", result3.Intent);

        var result4 = RegexExpenseParser.Parse("simular 400 4x");
        Assert.Equal("SIMULATE", result4.Intent);
        Assert.Equal(400.00m, result4.Amount);
        Assert.Equal(4, result4.Installments);

        var result5 = RegexExpenseParser.Parse("/desfazer");
        Assert.Equal("UNDO", result5.Intent);

        var result6 = RegexExpenseParser.Parse("pix 40 barbearia");
        Assert.Equal("ADD_EXPENSE", result6.Intent);
        Assert.Equal("Barbearia", result6.Description);
        Assert.Equal(40.00m, result6.Amount);
        Assert.Equal("PIX_CAIXINHA", result6.PaymentMethod);

        var result7 = RegexExpenseParser.Parse("paguei 25 no pix lanche");
        Assert.Equal("ADD_EXPENSE", result7.Intent);
        Assert.Equal("Lanche", result7.Description);
        Assert.Equal(25.00m, result7.Amount);
        Assert.Equal("Alimentação", result7.Category);
        Assert.Equal("PIX_CAIXINHA", result7.PaymentMethod);

        var result8 = RegexExpenseParser.Parse("tirei 100 da caixinha pro pix");
        Assert.Equal("ADD_EXPENSE", result8.Intent);
        Assert.Equal(100.00m, result8.Amount);
        Assert.Equal("PIX_CAIXINHA", result8.PaymentMethod);

        var result9 = RegexExpenseParser.Parse("fatura atual 350");
        Assert.Equal("SYNC_INVOICE", result9.Intent);
        Assert.Equal(350.00m, result9.Amount);

        var result10 = RegexExpenseParser.Parse("saldo caixinha 4500");
        Assert.Equal("UPDATE_CAIXINHA", result10.Intent);
        Assert.Equal(4500.00m, result10.Amount);

        var result11 = RegexExpenseParser.Parse("como ta minha fatura");
        Assert.Equal("STATUS", result11.Intent);
    }
}
