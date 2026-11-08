using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Domain.Services;
using FinanceTracker.Api.Services.Ai;
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
        var commitment = new FixedCommitment
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
        var commitment = new FixedCommitment
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
    }
}
