using FinanceTracker.Application.Models;
using FinanceTracker.Application.Services.Invoices;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Domain.UnitOfWorks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceTracker.Tests;

public class InvoiceServiceTests
{
    private class FakeUnitOfWork : IUnitOfWorkApp
    {
        public FakeFinancialProfileRepo FinancialProfileRepo { get; } = new();
        public FakeCardExpenseRepo CardExpenseRepo { get; } = new();
        public FakeFixedCommitmentRepo FixedCommitmentRepo { get; } = new();
        public FakeInvoicePaymentRepo InvoicePaymentRepo { get; } = new();

        public IFinancialProfileRepository FinancialProfile => FinancialProfileRepo;
        public ICardExpenseRepository CardExpense => CardExpenseRepo;
        public IFixedCommitmentRepository FixedCommitment => FixedCommitmentRepo;
        public IInvoicePaymentRepository InvoicePayment => InvoicePaymentRepo;
        public bool IsPostgres => true;

        public void BeginTransaction() { }
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }

    private class FakeFinancialProfileRepo : IFinancialProfileRepository
    {
        public FinancialProfileEntity CurrentProfile { get; set; } = new()
        {
            IncomeSecondTranche = 1000.00m,
            NubankCaixinhaBalance = 5000.00m,
            ClosingDay = 8
        };

        public Task<FinancialProfileEntity> GetProfileAsync() => Task.FromResult(CurrentProfile);
        public Task<bool> UpdateProfileAsync(FinancialProfileEntity profile) { CurrentProfile = profile; return Task.FromResult(true); }
        public Task<bool> UpdateCaixinhaBalanceAsync(decimal balance) { CurrentProfile.NubankCaixinhaBalance = balance; return Task.FromResult(true); }
        public Task<decimal> DepositCaixinhaAsync(decimal amount) { CurrentProfile.NubankCaixinhaBalance += amount; return Task.FromResult(CurrentProfile.NubankCaixinhaBalance); }
        public Task<bool> UpdateCardLimitAsync(decimal creditCardLimit, decimal? customCardCap) => Task.FromResult(true);
    }

    private class FakeCardExpenseRepo : ICardExpenseRepository
    {
        public List<CardExpenseEntity> Expenses { get; set; } = new();

        public Task<CardExpenseEntity?> GetByIdAsync(int id) => Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id));
        public Task<CardExpenseEntity?> GetByUuidAsync(Guid uuid) => Task.FromResult(Expenses.FirstOrDefault(x => x.Uuid == uuid));
        public Task<IEnumerable<CardExpenseEntity>> GetByCycleAsync(string cycle) => Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Where(x => x.BillingCycle == cycle).ToList());
        public Task<IEnumerable<CardExpenseEntity>> GetByGroupIdAsync(Guid purchaseGroupId) => Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Where(x => x.PurchaseGroupId == purchaseGroupId).ToList());
        public Task<IEnumerable<CardExpenseEntity>> GetExpensesInCyclesAsync(IEnumerable<string> cycles) => Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Where(x => cycles.Contains(x.BillingCycle)).ToList());
        public Task<IEnumerable<CardExpenseEntity>> GetRecentAsync(int count = 10) => Task.FromResult<IEnumerable<CardExpenseEntity>>(Expenses.Take(count).ToList());
        public Task<int> CreateAsync(CardExpenseEntity entity) { Expenses.Add(entity); return Task.FromResult(1); }
        public Task CreateBatchAsync(IEnumerable<CardExpenseEntity> entities) { Expenses.AddRange(entities); return Task.CompletedTask; }
        public Task<bool> UpdateAsync(CardExpenseEntity entity) => Task.FromResult(true);
        public Task<bool> UpdateGroupDetailsAsync(Guid purchaseGroupId, string description, string category) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<CardExpenseEntity?> GetLastExpenseAsync() => Task.FromResult(Expenses.LastOrDefault());
        public Task<int> DeleteByPurchaseGroupIdAsync(Guid purchaseGroupId) => Task.FromResult(1);
        public Task<decimal> GetFutureCommittedInstallmentsTotalAsync(string targetCycle) => Task.FromResult(0m);
        public Task<decimal> GetTotalCommittedCardExpensesFromCycleAsync(string fromCycle) => Task.FromResult(0m);
        public Task<Dictionary<string, decimal>> GetFutureCommittedInstallmentsByCycleAsync(string startCycle, int monthsAhead) => Task.FromResult(new Dictionary<string, decimal>());
    }

    private class FakeFixedCommitmentRepo : IFixedCommitmentRepository
    {
        public Task<FixedCommitmentEntity?> GetByIdAsync(int id) => Task.FromResult<FixedCommitmentEntity?>(null);
        public Task<IEnumerable<FixedCommitmentEntity>> GetAllAsync(bool onlyActive = true) => Task.FromResult<IEnumerable<FixedCommitmentEntity>>(new List<FixedCommitmentEntity>());
        public Task<int> CreateAsync(FixedCommitmentEntity entity) => Task.FromResult(1);
        public Task<bool> UpdateAsync(FixedCommitmentEntity entity) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<(bool success, bool isPaid, decimal balanceAfter)> ToggleCommitmentPaidAsync(int id, string cycle) => Task.FromResult((true, true, 5000m));
    }

    private class FakeInvoicePaymentRepo : IInvoicePaymentRepository
    {
        public InvoicePaymentEntity? StoredPayment { get; set; }

        public Task<InvoicePaymentEntity?> GetByCycleAsync(string cycle) =>
            Task.FromResult(StoredPayment?.BillingCycle == cycle ? StoredPayment : null);

        public Task<(bool isPaid, decimal paidAmount, decimal surplusAmount, bool surplusInvested, DateTime? paidAt)> TogglePaymentAsync(string cycle, decimal currentInvoiceTotal, decimal secondTrancheSalary)
        {
            if (StoredPayment != null && StoredPayment.IsPaid)
            {
                // Revert
                var prevPaid = StoredPayment.PaidAmount;
                var prevSurplus = StoredPayment.SurplusAmount;
                StoredPayment.IsPaid = false;
                StoredPayment.SurplusInvested = false;
                return Task.FromResult((false, prevPaid, prevSurplus, false, (DateTime?)null));
            }
            else
            {
                // Pay
                var surplus = Math.Max(0, secondTrancheSalary - currentInvoiceTotal);
                StoredPayment = new InvoicePaymentEntity
                {
                    BillingCycle = cycle,
                    IsPaid = true,
                    PaidAmount = currentInvoiceTotal,
                    SurplusAmount = surplus,
                    SurplusInvested = false,
                    PaidAt = DateTime.UtcNow
                };
                return Task.FromResult((true, currentInvoiceTotal, surplus, false, (DateTime?)StoredPayment.PaidAt));
            }
        }

        public Task<(bool success, decimal surplusInvestedAmount, decimal newCaixinhaBalance)> InvestSurplusAsync(string cycle) =>
            Task.FromResult((true, 100m, 5100m));
    }

    [Fact]
    public async Task TogglePaymentAsync_WhenUnpaid_ShouldMarkAsPaid()
    {
        var uow = new FakeUnitOfWork();
        uow.CardExpenseRepo.Expenses.Add(new CardExpenseEntity
        {
            BillingCycle = "2026-12",
            Amount = 400.00m,
            PaymentMethod = "CARD"
        });

        var service = new InvoiceService(uow, NullLogger<InvoiceService>.Instance);
        var result = await service.TogglePaymentAsync("2026-12");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsPaid);
        Assert.Equal(400.00m, result.Value.PaidAmount);
        Assert.Equal(600.00m, result.Value.SurplusAmount);
        Assert.Contains("PAGA", result.Value.Message);
    }

    [Fact]
    public async Task TogglePaymentAsync_WhenAlreadyPaid_ShouldRevertToPending()
    {
        var uow = new FakeUnitOfWork();
        uow.InvoicePaymentRepo.StoredPayment = new InvoicePaymentEntity
        {
            BillingCycle = "2026-12",
            IsPaid = true,
            PaidAmount = 400.00m,
            SurplusAmount = 600.00m,
            SurplusInvested = false
        };

        var service = new InvoiceService(uow, NullLogger<InvoiceService>.Instance);
        var result = await service.TogglePaymentAsync("2026-12");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsPaid);
        Assert.Contains("desmarcada", result.Value.Message);
    }
}
