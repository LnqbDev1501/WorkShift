using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IHolidayPayRuleRepo
{
    Task<HolidayPayRule?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<List<HolidayPayRule>> GetAllOrderedAsync(CancellationToken ct = default);

    Task<bool> IsReferencedInPayrollAsync(int holidayPayRuleId, CancellationToken ct = default);

    Task<bool> IsReferencedAsReplacedRuleAsync(int holidayPayRuleId, CancellationToken ct = default);

    Task<bool> HasScheduledPublishRuleAsync(CancellationToken ct = default);

    Task<HolidayPayRule?> GetLatestActiveOrInactiveRuleAsync(CancellationToken ct = default);

    Task<List<HolidayPayRule>> GetDuePublishedRulesAsync(DateOnly today, CancellationToken ct = default);

    Task<List<HolidayPayRule>> GetActiveRulesAsync(CancellationToken ct = default);

    Task<List<DateOnly>> GetActiveHolidayDatesFromAsync(DateOnly fromDate, CancellationToken ct = default);

    void Add(HolidayPayRule rule);

    void Remove(HolidayPayRule rule);

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);

    void WriteAudit(Guid? actor, string action, int id, object? before, object? after);
}

