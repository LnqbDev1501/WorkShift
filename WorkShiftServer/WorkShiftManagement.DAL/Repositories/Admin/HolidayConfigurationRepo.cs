using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Admin;

// Shared persistence boundary for calendar/rule changes and payroll invalidation.
public sealed class HolidayConfigurationRepo(AppDbContext db) : WorkShiftManagement.DAL.Interfaces.Admin.IPayrollChangeTransaction
{
    public IQueryable<HolidayPayRule> Rules => db.HolidayPayRules;
    public IQueryable<PayrollDetail> Details => db.PayrollDetails;
    public IQueryable<PublicHolidayConfig> Holidays => db.PublicHolidayConfigs;

    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginPayrollChangeAsync(CancellationToken ct = default)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        try { await LockAsync(ct); return tx; }
        catch { await tx.DisposeAsync(); throw; }
    }

    public async Task LockAsync(CancellationToken ct)
    {
        // Serialize configuration changes and approval/submission in a consistent order.
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.holiday_pay_rules, public.public_holiday_configs IN SHARE ROW EXCLUSIVE MODE", ct);
    }

    public async Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var result = await action();
        await tx.CommitAsync(ct);
        return result;
    }

    public Task<List<int>> InvalidateAsync(IReadOnlyCollection<DateOnly> dates, int? holidayId, Guid actor, CancellationToken ct)
    {
        // CALCULATED and later payrolls are frozen. Calendar changes apply to future
        // calculations; existing versions must go through explicit rejection/correction.
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new List<int>());
    }
    public async Task<List<DateOnly>> ActiveDatesFromAsync(DateOnly from, CancellationToken ct) =>
        await db.PublicHolidayConfigs.Where(h => h.IsActive && h.HolidayDate >= from).Select(h => h.HolidayDate).ToListAsync(ct);

    public Task<bool> IsLockedAsync(int id, DateOnly date, CancellationToken ct) => db.PayrollPeriods.AnyAsync(p =>
        (p.StartDate <= date && p.EndDate >= date || p.Payrolls.Any(w => w.PayrollDetails.Any(d => d.PublicHolidayId == id)))
        && (p.Status == "APPROVED" || p.Payrolls.Any(w => w.Status == "APPROVED" || w.Status == "PAID")), ct);

    public void Audit(Guid? actor, string action, string table, int id, object? before, object? after) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor,
        Action = action,
        TableName = table,
        RecordId = id.ToString(),
        OldValue = before == null ? null : JsonSerializer.Serialize(before),
        NewValue = after == null ? null : JsonSerializer.Serialize(after),
        CreatedAt = DateTime.UtcNow
    });
    public void Add(HolidayPayRule rule) => db.HolidayPayRules.Add(rule);
    public void Remove(HolidayPayRule rule) => db.HolidayPayRules.Remove(rule);
    public Task<int> SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public static InvalidOperationException Conflict(string message, string code)
    {
        var error = new InvalidOperationException(message);
        error.Data["code"] = code;
        return error;
    }
}

