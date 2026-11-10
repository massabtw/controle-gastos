using FinanceTracker.Application.Services.FinancialEngine;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Xunit;

namespace FinanceTracker.Tests;

public class FinancialEngineBankLimitTests
{
    private class FakeUnitOfWork : IUnitOfWorkApp
    {
        public IDbConnection Connection => throw new NotImplementedException();
        public IDbTransaction? Transaction => null;
        public bool IsPostgres => true;

        public IFinancialProfileRepository FinancialProfile { get; set; } = new FakeProfileRepo();
        public IFixedCommitmentRepository FixedCommitment { get; set; } = new FakeCommitmentRepo();
        public ICardExpenseRepository CardExpense { get; set; } = new FakeCardExpenseRepo();
        public IInvoicePaymentRepository InvoicePayment { get; set; } = new FakeInvoicePaymentRepo();

        public void BeginTransaction() { }
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }

    private class FakeProfileRepo : IFinancialProfileRepository
    {
        public FinancialProfileEntity Profile { get; set; } = new()
        {
            CreditCardLimit = 1200.00m,
            IncomeFirstTranche = 1200m,
            IncomeSecondTranche = 1000m,
            MonthlyInvestmentGoal = 1000m,
            ClosingDay = 8,
            DueDay = 16
        };

        public Task<FinancialProfileEntity> GetProfileAsync() => Task.FromResult(Profile);
        public Task<bool> UpdateProfileAsync(FinancialProfileEntity profile) { Profile = profile; return Task.FromResult(true); }
        public Task<bool> UpdateCaixinhaBalanceAsync(decimal balance) => Task.FromResult(true);
        public Task<decimal> DepositCaixinhaAsync(decimal amount) => Task.FromResult(5000m);
        public Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap) => Task.FromResult(true);
    }

    private class FakeCommitmentRepo : IFixedCommitmentRepository
    {
        public Task<IEnumerable<FixedCommitmentEntity>> GetAllAsync(bool onlyActive = true) =>
            Task.FromResult<IEnumerable<FixedCommitmentEntity>>(new List<FixedCommitmentEntity>());

        public Task<FixedCommitmentEntity?> GetByIdAsync(int id) => Task.FromResult<FixedCommitmentEntity?>(null);
        public Task<int> CreateAsync(FixedCommitmentEntity entity) => Task.FromResult(1);
        public Task<bool> UpdateAsync(FixedCommitmentEntity entity) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle) => Task.FromResult((true, true, 5000m));
    }

    private class FakeInvoicePaymentRepo : IInvoicePaymentRepository
    {
        public Dictionary<string, InvoicePaymentEntity> Payments { get; } = new();

        public Task<InvoicePaymentEntity?> GetByCycleAsync(string cycle) =>
            Task.FromResult(Payments.TryGetValue(cycle, out var p) ? p : null);

        public Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> TogglePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary) =>
            throw new NotImplementedException();

        public Task<(bool success, decimal surplusInvestedAmount, decimal newCaixinhaBalance)> InvestSurplusAsync(string cycle) =>
            throw new NotImplementedException();
    }

    private class FakeCardExpenseRepo : ICardExpenseRepository
    {
        public List<CardExpenseEntity> Expenses { get; } = new();
        public FakeInvoicePaymentRepo InvoicePaymentRepo { get; set; } = new();

        public Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle) =>
            Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Where(e => e.BillingCycle == cycle).ToList());

        public Task<decimal> GetTotalUnpaidCardExpensesAsync()
        {
            // Simulate: Sum of all CARD expenses in cycles where IsPaid != true
            var unpaidTotal = Expenses
                .Where(e => e.PaymentMethod == "CARD" &&
                            (!InvoicePaymentRepo.Payments.TryGetValue(e.BillingCycle, out var payment) || !payment.IsPaid))
                .Sum(e => e.Amount);

            return Task.FromResult(unpaidTotal);
        }

        public Task<CardExpenseEntity?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid) => throw new NotImplementedException();
        public Task<IEnumerable<CardExpenseEntity>> GetByGroupIdAsync(Guid purchaseGroupId) => throw new NotImplementedException();
        public Task<IEnumerable<CardExpenseEntity>> GetExpensesInCyclesAsync(IEnumerable<string> cycles) => throw new NotImplementedException();
        public Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10) => Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Take(count).ToList());
        public Task<int> CreateAsync(CardExpenseEntity entity) => Task.FromResult(1);
        public Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities) => Task.CompletedTask;
        public Task<bool> UpdateAsync(CardExpenseEntity entity) => Task.FromResult(true);
        public Task<bool> UpdateGroupDetailsAsync(Guid purchaseGroupId, string description, string category) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<CardExpenseEntity?> GetLastExpenseAsync() => Task.FromResult(Expenses.LastOrDefault());
        public Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId) => Task.FromResult(1);
        public Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle) => Task.FromResult(0m);
        public Task<decimal> GetTotalCommittedCardExpensesFromCycleAsync(string fromCycle) => Task.FromResult(0m);
        public Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead) => Task.FromResult(new Dictionary<string, decimal>());
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenPriorCycleIsUnpaid_AvailableBankLimitShouldBeSynchronizedAcrossBothCycles()
    {
        var uow = new FakeUnitOfWork();
        var cardRepo = (FakeCardExpenseRepo)uow.CardExpense;
        var paymentRepo = (FakeInvoicePaymentRepo)uow.InvoicePayment;
        cardRepo.InvoicePaymentRepo = paymentRepo;

        // 2026-11 spent: 970.20 (unpaid)
        cardRepo.Expenses.Add(new CardExpenseEntity
        {
            Id = 1,
            BillingCycle = "2026-11",
            Amount = 970.20m,
            PaymentMethod = "CARD"
        });

        // 2026-12 spent: 94.70 (unpaid)
        cardRepo.Expenses.Add(new CardExpenseEntity
        {
            Id = 2,
            BillingCycle = "2026-12",
            Amount = 94.70m,
            PaymentMethod = "CARD"
        });

        var service = new FinancialEngineService(uow, NullLogger<FinancialEngineService>.Instance);

        // 1. View 2026-11: Limite Banco Livre should be 1200 - 970.20 - 94.70 = 135.10
        var summary202611 = (await service.GetDashboardSummaryAsync("2026-11")).Value!;
        Assert.Equal(135.10m, summary202611.AvailableBankCreditLimit);
        Assert.Equal(1064.90m, summary202611.TotalUnpaidCardUsage);

        // 2. View 2026-12: Limite Banco Livre MUST ALSO BE 135.10 (synchronized!)
        var summary202612 = (await service.GetDashboardSummaryAsync("2026-12")).Value!;
        Assert.Equal(135.10m, summary202612.AvailableBankCreditLimit);
        Assert.Equal(1064.90m, summary202612.TotalUnpaidCardUsage);

        // 3. Mark 2026-11 as PAID: 970.20 is freed up immediately!
        paymentRepo.Payments["2026-11"] = new InvoicePaymentEntity
        {
            BillingCycle = "2026-11",
            IsPaid = true,
            PaidAmount = 970.20m
        };

        // Available bank limit becomes 1200 - 94.70 = 1105.30 in BOTH cycles
        var summary202611AfterPaid = (await service.GetDashboardSummaryAsync("2026-11")).Value!;
        Assert.Equal(1105.30m, summary202611AfterPaid.AvailableBankCreditLimit);
        Assert.Equal(94.70m, summary202611AfterPaid.TotalUnpaidCardUsage);

        var summary202612AfterPaid = (await service.GetDashboardSummaryAsync("2026-12")).Value!;
        Assert.Equal(1105.30m, summary202612AfterPaid.AvailableBankCreditLimit);
        Assert.Equal(94.70m, summary202612AfterPaid.TotalUnpaidCardUsage);
    }
}
