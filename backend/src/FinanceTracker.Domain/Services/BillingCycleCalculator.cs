using System.Globalization;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Services;

public static class BillingCycleCalculator
{
    public static string GetTargetBillingCycle(DateTime date, int closingDay = 8)
    {
        var targetDate = date.Day >= closingDay
            ? new DateTime(date.Year, date.Month, 1).AddMonths(1)
            : new DateTime(date.Year, date.Month, 1);

        return targetDate.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    public static (DateTime ClosingDate, DateTime DueDate) GetCycleDates(string cycle, int closingDay = 8, int dueDay = 16)
    {
        var (year, month) = ParseCycle(cycle);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var actualClosingDay = Math.Min(closingDay, daysInMonth);
        var actualDueDay = Math.Min(dueDay, daysInMonth);

        var closingDate = new DateTime(year, month, actualClosingDay);
        var dueDate = new DateTime(year, month, actualDueDay);

        return (closingDate, dueDate);
    }

    public static (DateTime StartDate, DateTime EndDate) GetCyclePeriod(string cycle, int closingDay = 8)
    {
        var (year, month) = ParseCycle(cycle);
        var cycleDate = new DateTime(year, month, 1);
        var prevCycleDate = cycleDate.AddMonths(-1);

        var daysInPrevMonth = DateTime.DaysInMonth(prevCycleDate.Year, prevCycleDate.Month);
        var daysInCurrentMonth = DateTime.DaysInMonth(year, month);

        var startDate = new DateTime(prevCycleDate.Year, prevCycleDate.Month, Math.Min(closingDay, daysInPrevMonth));
        var endDate = new DateTime(year, month, Math.Min(closingDay, daysInCurrentMonth));

        return (startDate, endDate);
    }

    public static int GetDaysUntilClosing(DateTime currentDate, int closingDay, string? targetCycle = null)
    {
        var currentActiveCycle = GetTargetBillingCycle(currentDate, closingDay);
        var inspectedCycle = targetCycle ?? currentActiveCycle;

        var comparison = string.CompareOrdinal(inspectedCycle, currentActiveCycle);

        if (comparison < 0)
        {
            return 0;
        }

        if (comparison > 0)
        {
            var (startDate, endDate) = GetCyclePeriod(inspectedCycle, closingDay);
            return Math.Max(1, (endDate - startDate).Days);
        }

        var (closingDate, _) = GetCycleDates(inspectedCycle, closingDay);
        var daysRemaining = (closingDate.Date - currentDate.Date).Days;

        return Math.Max(1, daysRemaining);
    }

    public static string? GetCommitmentInstallmentStatus(FixedCommitmentEntity commitment, string targetCycle)
    {
        if (!commitment.IsActive)
            return null;

        var (startYear, startMonth) = ParseCycle(commitment.StartCycle);
        var (targetYear, targetMonth) = ParseCycle(targetCycle);

        var monthDiff = (targetYear - startYear) * 12 + (targetMonth - startMonth);

        if (commitment.TotalInstallments == null)
        {
            return monthDiff >= 0 ? "Fixo" : null;
        }

        var total = commitment.TotalInstallments.Value;
        if (monthDiff >= 0 && monthDiff < total)
        {
            return $"{monthDiff + 1}/{total}";
        }

        return null;
    }

    public static string AddMonthsToCycle(string cycle, int months)
    {
        var (year, month) = ParseCycle(cycle);
        var date = new DateTime(year, month, 1).AddMonths(months);
        return date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    public static (int Year, int Month) ParseCycle(string cycle)
    {
        if (string.IsNullOrWhiteSpace(cycle) || cycle.Length != 7 || cycle[4] != '-')
        {
            throw new ArgumentException($"Invalid billing cycle format: '{cycle}'. Expected 'YYYY-MM'.", nameof(cycle));
        }

        var year = int.Parse(cycle.Substring(0, 4), CultureInfo.InvariantCulture);
        var month = int.Parse(cycle.Substring(5, 2), CultureInfo.InvariantCulture);

        return (year, month);
    }

    public static DateTime GetSubscriptionChargeDateForCycle(string cycle, int billingDay, int closingDay = 8)
    {
        var (year, month) = ParseCycle(cycle);
        var cycleDate = new DateTime(year, month, 1);
        var targetMonthDate = billingDay >= closingDay ? cycleDate.AddMonths(-1) : cycleDate;
        var daysInMonth = DateTime.DaysInMonth(targetMonthDate.Year, targetMonthDate.Month);
        var actualDay = Math.Clamp(billingDay, 1, daysInMonth);
        return new DateTime(targetMonthDate.Year, targetMonthDate.Month, actualDay);
    }

    public static DateTime GetCurrentBrazilDate(DateTime? utcNow = null)
    {
        var utc = utcNow ?? DateTime.UtcNow;
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        }
        catch
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
            }
            catch
            {
                return utc.AddHours(-3);
            }
        }
    }
}
