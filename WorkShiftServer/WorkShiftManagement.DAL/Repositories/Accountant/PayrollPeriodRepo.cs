using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayrollPeriodRepo(AppDbContext db) : IPayrollPeriodRepo
{
    public Task<User?> GetActorAsync(
        Guid id,
        CancellationToken ct) =>
        db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(
                u => u.UserId == id,
                ct);

    private static IQueryable<PayrollPeriod> Details(
        IQueryable<PayrollPeriod> query) =>
        query
            .Include(p => p.ScheduleWeek)
                .ThenInclude(w => w!.Branch)
            .Include(p => p.Timesheets)
            .Include(p => p.EmployeeRewardsPenalties)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PayrollDetails)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PayrollCorrectionRequestPayrolls)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.EmployeeType)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PaymentConfirmedByNavigation)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.CalculatedByNavigation)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PayrollCarryForwardApplications)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PayrollCarryForwardDeductions)
            .AsSplitQuery();

    private static IQueryable<PayrollPeriod> ListSummary(
        IQueryable<PayrollPeriod> query) =>
        query
            .Include(p => p.ScheduleWeek)
                .ThenInclude(w => w!.Branch)
            .Include(p => p.Timesheets)
            .Include(p => p.EmployeeRewardsPenalties)
            .Include(p => p.Payrolls)
                .ThenInclude(p => p.PayrollCorrectionRequestPayrolls)
            .AsSplitQuery();

    public async Task<(List<PayrollPeriod> Items, int Count)> GetPagedAsync(
        int branchId,
        string? search,
        string? status,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = db.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.BranchId == branchId);

        if (status != null)
            query = query.Where(p => p.Status == status);

        if (from.HasValue)
            query = query.Where(p => p.StartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.StartDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search
                .Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            query = query.Where(
                p => EF.Functions.ILike(
                         p.PeriodCode,
                         $"%{keyword}%",
                         "\\")
                     || EF.Functions.ILike(
                         p.PeriodName,
                         $"%{keyword}%",
                         "\\"));
        }

        var count = await query.CountAsync(ct);

        var items = await ListSummary(
                query
                    .OrderByDescending(p => p.StartDate)
                    .ThenByDescending(p => p.PayrollPeriodId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
            .ToListAsync(ct);

        return (items, count);
    }

    public async Task<(List<WorkScheduleWeek> Items, int Count)> GetAvailableWeeksAsync(
        int branchId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = db.WorkScheduleWeeks
            .AsNoTracking()
            .Where(
                w => w.BranchId == branchId
                     && w.Status != "CANCELLED"
                     && !db.PayrollPeriods.Any(
                         p => p.ScheduleWeekId == w.ScheduleWeekId));

        if (from.HasValue)
            query = query.Where(w => w.WeekStartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(w => w.WeekStartDate <= to.Value);

        var count = await query.CountAsync(ct);

        return (
            await query
                .OrderByDescending(w => w.WeekStartDate)
                .ThenByDescending(w => w.ScheduleWeekId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct),
            count);
    }

    public async Task<PayrollPeriod?> GetByIdAsync(
        int id,
        int? branchId,
        bool tracking,
        CancellationToken ct)
    {
        if (tracking)
        {
            if (db.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException(
                    "A transaction is required to lock the period.");
            }

            // A parent row lock also prevents a concurrent FK insert from racing the dependency check.
            await db.PayrollPeriods
                .FromSqlInterpolated(
                    $"SELECT * FROM public.payroll_periods WHERE payroll_period_id = {id} FOR UPDATE")
                .AsTracking()
                .ToListAsync(ct);
        }

        var query = tracking
            ? db.PayrollPeriods.AsTracking()
            : db.PayrollPeriods.AsNoTracking();

        if (branchId.HasValue)
            query = query.Where(p => p.BranchId == branchId.Value);

        return await Details(query)
            .SingleOrDefaultAsync(
                p => p.PayrollPeriodId == id,
                ct);
    }

    public async Task<WorkScheduleWeek?> GetWeekAsync(
        int id,
        int branchId,
        bool lockRow,
        CancellationToken ct)
    {
        if (lockRow)
        {
            var rows = await db.WorkScheduleWeeks
                .FromSqlInterpolated(
                    $"SELECT * FROM public.work_schedule_weeks WHERE schedule_week_id = {id} AND branch_id = {branchId} FOR UPDATE")
                .AsTracking()
                .ToListAsync(ct);

            var week = rows.SingleOrDefault();

            if (week != null)
            {
                await db.Entry(week)
                    .Reference(w => w.Branch)
                    .LoadAsync(ct);
            }

            return week;
        }

        return await db.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.Branch)
            .SingleOrDefaultAsync(
                w => w.ScheduleWeekId == id
                     && w.BranchId == branchId,
                ct);
    }

    public Task<bool> WeekHasPeriodAsync(
        int weekId,
        int? excludingId,
        CancellationToken ct) =>
        db.PayrollPeriods.AnyAsync(
            p => p.ScheduleWeekId == weekId
                 && (!excludingId.HasValue
                     || p.PayrollPeriodId != excludingId.Value),
            ct);

    public async Task<bool> HasDependenciesAsync(
        int id,
        CancellationToken ct) =>
        await db.Timesheets.AnyAsync(
            t => t.PayrollPeriodId == id,
            ct)
        || await db.Payrolls.AnyAsync(
            p => p.PayrollPeriodId == id,
            ct)
        || await db.EmployeeRewardsPenalties.AnyAsync(
            a => a.PayrollPeriodId == id,
            ct);

    public Task<int> ExpectedEmployeeCountAsync(
        PayrollPeriod period,
        CancellationToken ct) =>
        db.Timesheets
            .Where(t => t.PayrollPeriodId == period.PayrollPeriodId)
            .Select(t => t.UserId)
            .Union(
                db.ShiftAssignments
                    .Where(
                        a => a.Shift.ScheduleWeekId == period.ScheduleWeekId
                             && a.Status != "CANCELLED"
                             && a.Shift.Status != "CANCELLED")
                    .Select(a => a.UserId))
            .Distinct()
            .CountAsync(ct);

    public async Task<Dictionary<int, int>> GetExpectedEmployeeCountsAsync(
        IReadOnlyList<PayrollPeriod> periods,
        CancellationToken ct)
    {
        if (periods.Count == 0) return new Dictionary<int, int>();

        var periodIds = periods.Select(p => p.PayrollPeriodId).ToList();
        var weekIds = periods.Where(p => p.ScheduleWeekId.HasValue).Select(p => p.ScheduleWeekId!.Value).Distinct().ToList();

        var timesheetUsers = await db.Timesheets
            .AsNoTracking()
            .Where(t => periodIds.Contains(t.PayrollPeriodId))
            .Select(t => new { t.PayrollPeriodId, t.UserId })
            .Distinct()
            .ToListAsync(ct);

        var shiftUsers = weekIds.Count > 0
            ? await db.ShiftAssignments
                .AsNoTracking()
                .Where(a => weekIds.Contains(a.Shift.ScheduleWeekId)
                            && a.Status != "CANCELLED"
                            && a.Shift.Status != "CANCELLED")
                .Select(a => new { a.Shift.ScheduleWeekId, a.UserId })
                .Distinct()
                .ToListAsync(ct)
            : [];

        var timesheetLookup = timesheetUsers
            .GroupBy(t => t.PayrollPeriodId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToHashSet());

        var shiftLookup = shiftUsers
            .GroupBy(s => s.ScheduleWeekId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToHashSet());

        var result = new Dictionary<int, int>(periods.Count);
        foreach (var p in periods)
        {
            var userIds = new HashSet<Guid>();
            if (timesheetLookup.TryGetValue(p.PayrollPeriodId, out var tsSet))
                userIds.UnionWith(tsSet);
            if (p.ScheduleWeekId.HasValue && shiftLookup.TryGetValue(p.ScheduleWeekId.Value, out var saSet))
                userIds.UnionWith(saSet);
            result[p.PayrollPeriodId] = userIds.Count;
        }

        return result;
    }

    public async Task<IDbContextTransaction> BeginWriteAsync(
        CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Same lock order as existing payroll approval/configuration writes.
            await db.Database.ExecuteSqlRawAsync(
                "LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE",
                ct);

            return tx;
        }
        catch
        {
            await tx.DisposeAsync();
            throw;
        }
    }

    public void Add(PayrollPeriod period) =>
        db.PayrollPeriods.Add(period);

    public void Remove(PayrollPeriod period) =>
        db.PayrollPeriods.Remove(period);

    public void Audit(
        Guid actorId,
        string action,
        PayrollPeriod period,
        object? before) =>
        db.AuditLogs.Add(
            new AuditLog
            {
                UserId = actorId,
                Action = action,
                TableName = "payroll_periods",
                RecordId = period.PayrollPeriodId.ToString(),

                OldValue = before == null
                    ? null
                    : JsonSerializer.Serialize(before),

                NewValue = action == "DELETE_PAYROLL_PERIOD"
                    ? null
                    : JsonSerializer.Serialize(
                        new
                        {
                            period.PeriodCode,
                            period.PeriodName,
                            period.ScheduleWeekId,
                            period.Status,
                            period.StartDate,
                            period.EndDate
                        }),

                CreatedAt = DateTime.UtcNow
            });

    public async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                  && pg.SqlState == "23505")
        {
            throw new InvalidOperationException(
                "This schedule week or period code already has a payroll period.",
                ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                  && pg.SqlState == "23503")
        {
            throw new InvalidOperationException(
                "The period has dependent data or a related record changed. Reload and try again.",
                ex);
        }
    }
}