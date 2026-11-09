using FinanceTracker.Application.Services.Ai;
using Xunit;

namespace FinanceTracker.Tests;

public class UnitTest1
{
    [Theory]
    [InlineData("+10 caixinha", 10.0)]
    [InlineData("+ 50 na caixinha", 50.0)]
    [InlineData("guardar 100 na caixinha", 100.0)]
    [InlineData("adicionar 25 na caixinha", 25.0)]
    [InlineData("botar 10 na caixinha", 10.0)]
    public void Should_Parse_Deposit_Caixinha(string input, decimal expectedAmount)
    {
        var result = RegexExpenseParser.Parse(input);
        Assert.Equal("DEPOSIT_CAIXINHA", result.Intent);
        Assert.Equal(expectedAmount, result.Amount);
    }

    [Theory]
    [InlineData("recebi 50 pix do joao", 50.0, "Pix João")]
    [InlineData("recebi pix 30 lanche", 30.0, "Pix Lanche")]
    [InlineData("recebi 100 de reembolso", 100.0, "Pix Reembolso")]
    [InlineData("pix recebido 45", 45.0, "Pix Recebido")]
    public void Should_Parse_Receive_Pix(string input, decimal expectedAmount, string expectedPrefix)
    {
        var result = RegexExpenseParser.Parse(input);
        Assert.Equal("RECEIVE_PIX", result.Intent);
        Assert.Equal(expectedAmount, result.Amount);
        Assert.Equal("PIX_INFLOW", result.PaymentMethod);
        Assert.StartsWith(expectedPrefix.Split(' ')[0], result.Description);
    }
}
