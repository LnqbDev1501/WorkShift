using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class HolidayPayRuleRepo(AppDbContext db) : IHolidayPayRuleRepo
{
    public Task<HolidayPayRule?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.HolidayPayRules.SingleOrDefaultAsync(r => r.HolidayPayRuleId == id, ct);

    public Task<List<HolidayPayRule>> GetAllOrderedAsync(CancellationToken ct = default) =>
        db.HolidayPayRules.AsNoTracking()
            .OrderByDescending(r => r.EffectiveFrom)
            .ThenByDescending(r => r.HolidayPayRuleId)
            .ToListAsync(ct);

    public Task<bool> IsReferencedInPayrollAsync(int holidayPayRuleId, CancellationToken ct = default) =>
        db.PayrollDetails.AnyAsync(d => d.HolidayPayRuleId == holidayPayRuleId, ct);

    public Task<bool> IsReferencedAsReplacedRuleAsync(int holidayPayRuleId, CancellationToken ct = default) =>
        db.HolidayPayRules.AnyAsync(r => r.ReplacedRuleId == holidayPayRuleId, ct);

    public Task<bool> HasScheduledPublishRuleAsync(CancellationToken ct = default) =>
        db.HolidayPayRules.AnyAsync(r => r.Status == "PUBLISH", ct);

    public Task<HolidayPayRule?> GetLatestActiveOrInactiveRuleAsync(CancellationToken ct = default) =>
        db.HolidayPayRules
            .Where(r => r.Status == "ACTIVE" || r.Status == "INACTIVE")
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public Task<List<HolidayPayRule>> GetDuePublishedRulesAsync(DateOnly today, CancellationToken ct = default) =>
        db.HolidayPayRules
            .Where(r => r.Status == "PUBLISH" && r.EffectiveFrom <= today)
            .OrderBy(r => r.EffectiveFrom)
            .ToListAsync(ct);

    public Task<List<HolidayPayRule>> GetActiveRulesAsync(CancellationToken ct = default) =>
        db.HolidayPayRules
            .Where(r => r.Status == "ACTIVE")
            .ToListAsync(ct);

    public Task<List<DateOnly>> GetActiveHolidayDatesFromAsync(DateOnly fromDate, CancellationToken ct = default) =>
        db.PublicHolidayConfigs
            .Where(h => h.IsActive && h.HolidayDate >= fromDate)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);

    public void Add(HolidayPayRule rule) => db.HolidayPayRules.Add(rule);

    public void Remove(HolidayPayRule rule) => db.HolidayPayRules.Remove(rule);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.holiday_pay_rules, public.public_holiday_configs IN SHARE ROW EXCLUSIVE MODE", ct);
        var result = await action();
        await tx.CommitAsync(ct);
        return result;
    }

    public void WriteAudit(Guid? actor, string action, int id, object? before, object? after) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor,
        Action = action,
        TableName = "holiday_pay_rules",
        RecordId = id.ToString(),
        OldValue = before == null ? null : JsonSerializer.Serialize(before),
        NewValue = after == null ? null : JsonSerializer.Serialize(after),
        CreatedAt = DateTime.UtcNow
    });
}

