using FinanceTracker.Api.Domain.Dtos;
using FinanceTracker.Api.Domain.Entities;
using FinanceTracker.Api.Domain.Services;
using FinanceTracker.Api.Infrastructure.Repositories;

namespace FinanceTracker.Api.Services;

public interface IExpenseService
{
    Task<IEnumerable<CardExpense>> GetExpensesByCycleAsync(string? cycle = null);
    Task<CardExpense?> GetExpenseByIdAsync(int id);
    Task<List<CardExpense>> CreateExpenseAsync(CreateExpenseDto dto);
    Task<bool> UpdateExpenseAsync(int id, UpdateExpenseDto dto);
    Task<bool> DeleteExpenseAsync(int id, bool deleteAllInstallments = false);
    Task<string> UndoLatestExpenseAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IProfileRepository _profileRepository;

    public ExpenseService(IExpenseRepository expenseRepository, IProfileRepository profileRepository)
    {
        _expenseRepository = expenseRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IEnumerable<CardExpense>> GetExpensesByCycleAsync(string? cycle = null)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var targetCycle = string.IsNullOrWhiteSpace(cycle)
            ? BillingCycleCalculator.GetTargetBillingCycle(DateTime.Now, profile.ClosingDay)
            : cycle;

        return await _expenseRepository.GetExpensesByCycleAsync(targetCycle);
    }

    public async Task<CardExpense?> GetExpenseByIdAsync(int id)
    {
        return await _expenseRepository.GetExpenseByIdAsync(id);
    }

    public async Task<List<CardExpense>> CreateExpenseAsync(CreateExpenseDto dto)
    {
        var profile = await _profileRepository.GetProfileAsync();
        var txDate = dto.TransactionDate ?? DateTime.Now;
        var startCycle = BillingCycleCalculator.GetTargetBillingCycle(txDate, profile.ClosingDay);

        var installments = Math.Max(1, dto.Installments);
        var createdExpenses = new List<CardExpense>();

        if (installments == 1)
        {
            var single = new CardExpense
            {
                Description = dto.Description.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category.Trim(),
                Amount = dto.TotalAmount,
                TransactionDate = txDate,
                BillingCycle = startCycle,
                InstallmentCurrent = 1,
                InstallmentTotal = 1,
                PurchaseGroupId = null
            };

            var id = await _expenseRepository.AddExpenseAsync(single);
            single.Id = id;
            createdExpenses.Add(single);
        }
        else
        {
            var groupId = Guid.NewGuid();
            // Calculate base installment amount with 2 decimal places
            var baseAmount = Math.Floor((dto.TotalAmount / installments) * 100m) / 100m;
            var remainder = dto.TotalAmount - (baseAmount * installments);

            for (int i = 1; i <= installments; i++)
            {
                var cycle = BillingCycleCalculator.AddMonthsToCycle(startCycle, i - 1);
                // 1st installment absorbs the remainder cents so the sum is exact
                var installmentAmount = (i == 1) ? baseAmount + remainder : baseAmount;

                var expense = new CardExpense
                {
                    Description = dto.Description.Trim(),
                    Category = string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category.Trim(),
                    Amount = installmentAmount,
                    TransactionDate = txDate,
                    BillingCycle = cycle,
                    InstallmentCurrent = i,
                    InstallmentTotal = installments,
                    PurchaseGroupId = groupId
                };

                createdExpenses.Add(expense);
            }

            await _expenseRepository.AddExpensesBatchAsync(createdExpenses);
        }

        return createdExpenses;
    }

    public async Task<bool> UpdateExpenseAsync(int id, UpdateExpenseDto dto)
    {
        var existing = await _expenseRepository.GetExpenseByIdAsync(id);
        if (existing == null)
            return false;

        var profile = await _profileRepository.GetProfileAsync();
        var targetCycle = BillingCycleCalculator.GetTargetBillingCycle(dto.TransactionDate, profile.ClosingDay);

        if (dto.UpdateAllGroupInstallments && existing.PurchaseGroupId.HasValue)
        {
            // Update description and category across all installments of the group
            await _expenseRepository.UpdateExpenseGroupDetailsAsync(
                existing.PurchaseGroupId.Value,
                dto.Description.Trim(),
                string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category.Trim()
            );

            // Also update amount / date for this specific installment
            existing.Amount = dto.Amount;
            existing.TransactionDate = dto.TransactionDate;
            existing.BillingCycle = targetCycle;
            return await _expenseRepository.UpdateExpenseAsync(existing);
        }

        existing.Description = dto.Description.Trim();
        existing.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category.Trim();
        existing.Amount = dto.Amount;
        existing.TransactionDate = dto.TransactionDate;
        existing.BillingCycle = targetCycle;

        return await _expenseRepository.UpdateExpenseAsync(existing);
    }

    public async Task<bool> DeleteExpenseAsync(int id, bool deleteAllInstallments = false)
    {
        var existing = await _expenseRepository.GetExpenseByIdAsync(id);
        if (existing == null)
            return false;

        if (deleteAllInstallments && existing.PurchaseGroupId.HasValue)
        {
            var count = await _expenseRepository.DeleteExpenseGroupAsync(existing.PurchaseGroupId.Value);
            return count > 0;
        }

        return await _expenseRepository.DeleteExpenseAsync(id);
    }

    public async Task<string> UndoLatestExpenseAsync()
    {
        var latest = await _expenseRepository.GetLatestExpenseAsync();
        if (latest == null)
            return "Nenhum lançamento recente encontrado para desfazer.";

        if (latest.PurchaseGroupId.HasValue)
        {
            var group = (await _expenseRepository.GetExpensesByGroupIdAsync(latest.PurchaseGroupId.Value)).ToList();
            var total = group.Sum(x => x.Amount);
            var desc = group.FirstOrDefault()?.Description ?? latest.Description;
            var installments = group.Count;

            await _expenseRepository.DeleteExpenseGroupAsync(latest.PurchaseGroupId.Value);
            return $"Desfeito com sucesso: Compra '{desc}' (R$ {total:F2} em {installments}x).";
        }
        else
        {
            await _expenseRepository.DeleteExpenseAsync(latest.Id);
            return $"Desfeito com sucesso: Gasto '{latest.Description}' (R$ {latest.Amount:F2}).";
        }
    }
}
