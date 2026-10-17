using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayrollCalculationRepo(AppDbContext db) : IPayrollCalculationRepo
{
    private List<NumericColumn>? numericColumns;
    public sealed class NumericColumn
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public int? Scale { get; set; }
    }
    public Task<User?> GetActorAsync(Guid id, CancellationToken ct) => db.Users.AsNoTracking()
        .Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == id, ct);

    public Task<PayrollPeriod?> GetPeriodAsync(int id, int? branchId, CancellationToken ct) => db.PayrollPeriods
        .SingleOrDefaultAsync(p => p.PayrollPeriodId == id && (!branchId.HasValue || p.BranchId == branchId), ct);

    public async Task<PayrollCalculationInputs> GetInputsAsync(PayrollPeriod period, CancellationToken ct)
    {
        var timesheets = await db.Timesheets.AsNoTracking().Include(t => t.User).Include(t => t.TimesheetAdjustmentRequests)
            .Where(t => t.PayrollPeriodId == period.PayrollPeriodId).ToListAsync(ct);
        var assignments = await db.ShiftAssignments.AsNoTracking().Include(a => a.Shift).Include(a => a.User)
            .Include(a => a.Attendance).ThenInclude(a => a!.AttendancePenaltyRecords)
            .Where(a => a.Shift.ScheduleWeekId == period.ScheduleWeekId
                && a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED").AsSplitQuery().ToListAsync(ct);
        var ids = timesheets.Select(t => t.UserId).Union(assignments.Select(a => a.UserId)).ToList();
        return new PayrollCalculationInputs
        {
            Period = period, EmployeeIds = ids, Timesheets = timesheets, Assignments = assignments,
            Salaries = await db.StaffSalaryHistories.AsNoTracking().Include(s => s.Contract)
                .Where(s => ids.Contains(s.UserId) && s.EffectiveFrom <= period.EndDate
                    && (s.EffectiveTo == null || s.EffectiveTo >= period.StartDate)).ToListAsync(ct),
            Adjustments = await db.EmployeeRewardsPenalties.AsNoTracking()
                .Where(a => a.PayrollPeriodId == period.PayrollPeriodId && !a.IsDeleted).ToListAsync(ct),
            Holidays = await db.PublicHolidayConfigs.AsNoTracking().Where(h => h.IsActive
                && h.HolidayDate >= period.StartDate && h.HolidayDate <= period.EndDate).ToListAsync(ct),
            HolidayRules = await db.HolidayPayRules.AsNoTracking().Where(r => r.EffectiveFrom <= period.EndDate
                && (r.EffectiveTo == null || r.EffectiveTo > period.StartDate)).ToListAsync(ct),
            CarryForwards = await db.PayrollCarryForwardDeductions.AsNoTracking().Include(c => c.SourcePayroll).ThenInclude(p => p.PayrollPeriod)
                .Include(c => c.SourcePayrollDetail).Where(c => ids.Contains(c.UserId)
                    && (c.Status == "OPEN" || c.Status == "PARTIALLY_APPLIED") && c.RemainingAmount > 0)
                .OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId).ToListAsync(ct),
            UnsettledPriorEmployees = (await db.Timesheets.AsNoTracking()
                .Where(t => ids.Contains(t.UserId) && t.PayrollPeriod.StartDate < period.StartDate
                    && t.PayrollPeriod.Status != "PAID" && t.PayrollPeriod.Status != "CLOSED")
                .Select(t => t.UserId).Union(db.ShiftAssignments.Where(a => ids.Contains(a.UserId)
                    && a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED"
                    && db.PayrollPeriods.Any(p => p.ScheduleWeekId == a.Shift.ScheduleWeekId
                        && p.StartDate < period.StartDate && p.Status != "PAID" && p.Status != "CLOSED"))
                    .Select(a => a.UserId)).Distinct().ToListAsync(ct)).ToHashSet(),
            Payrolls = await db.Payrolls.AsNoTracking().Include(p => p.PayrollDetails)
                .Where(p => p.PayrollPeriodId == period.PayrollPeriodId).ToListAsync(ct)
        };
    }

    public async Task<IDbContextTransaction> BeginCalculationAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // All payroll/review mutations share this lock order; initial calculation is one transaction.
            db.ChangeTracker.Clear();
            // Match approval lock order. Serialize retries, settlement and configuration changes.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_feedback_requests, public.payroll_correction_requests, public.payslips, public.payroll_carry_forward_deductions, public.payroll_carry_forward_applications IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.timesheets, public.shift_assignments, public.work_shifts, public.attendances, public.attendance_penalty_records, public.staff_salary_history, public.employment_contracts, public.employee_rewards_penalties, public.public_holiday_configs, public.holiday_pay_rules IN SHARE MODE", ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }

    public Task<Payroll?> GetPayrollAsync(int id, int? branchId, CancellationToken ct) => db.Payrolls.AsNoTracking()
        .Include(p => p.PayrollDetails).Include(p => p.PayrollPeriod)
        .SingleOrDefaultAsync(p => p.PayrollId == id && (!branchId.HasValue || p.BranchId == branchId), ct);

    private IQueryable<Payroll> CurrentQuery(int periodId)
    {
        var query = db.Payrolls.AsNoTracking().Where(p => p.PayrollPeriodId == periodId && p.Status != "REPLACED"
            && ((p.PayrollPeriod.Status != "APPROVED" && p.PayrollPeriod.Status != "PAID" && p.PayrollPeriod.Status != "CLOSED")
                || p.Status == "APPROVED" || p.Status == "PAID"));
        // Select current versions BEFORE filtering, so an older matching status cannot reappear.
        query = query.Where(p => !db.Payrolls.Any(n => n.PayrollPeriodId == periodId && n.UserId == p.UserId
            && n.Status != "REPLACED" && (n.VersionNo > p.VersionNo || n.VersionNo == p.VersionNo && n.PayrollId > p.PayrollId)
            && ((n.PayrollPeriod.Status != "APPROVED" && n.PayrollPeriod.Status != "PAID" && n.PayrollPeriod.Status != "CLOSED")
                || n.Status == "APPROVED" || n.Status == "PAID")));
        return query;
    }

    public async Task<(List<Payroll> Items, int Count)> GetPayrollsAsync(int periodId, string? keyword, Guid? userId,
        string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = CurrentQuery(periodId);
        if (userId.HasValue) query = query.Where(p => p.UserId == userId);
        if (status != null) query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = "%" + keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var isId = Guid.TryParse(keyword.Trim(), out var id);
            query = query.Where(p => EF.Functions.ILike(p.EmployeeFullNameSnapshot, term, "\\") || isId && p.UserId == id);
        }
        var count = await query.CountAsync(ct);
        return (await query.OrderBy(p => p.EmployeeFullNameSnapshot).ThenBy(p => p.UserId)
            .Skip((page - 1) * pageSize).Take(pageSize).Include(p => p.PayrollDetails).ToListAsync(ct), count);
    }

    public void Add(Payroll payroll) => db.Payrolls.Add(payroll);
    public void Audit(Guid actor, Payroll payroll) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor, Action = "CALCULATE_EMPLOYEE_PAYROLL", TableName = "payrolls",
        RecordId = payroll.PayrollId.ToString(), CreatedAt = DateTime.UtcNow,
        NewValue = JsonSerializer.Serialize(new { payroll.PayrollPeriodId, payroll.UserId, payroll.TimesheetId,
            payroll.VersionNo, payroll.GrossSalary, payroll.TotalDeduction, payroll.NetSalary })
    });
    public async Task SaveAsync(CancellationToken ct)
    {
        // Supplied schema uses NUMERIC, but protect installations still using NUMERIC(p,2):
        // reject unrepresentable values instead of letting PostgreSQL round money/hours silently.
        static bool RequiresNumericCheck(object entity) => entity is Payroll or PayrollDetail or PayrollCarryForwardDeduction or PayrollCarryForwardApplication;
        if (db.ChangeTracker.Entries().Any(e => e.State is EntityState.Added or EntityState.Modified && RequiresNumericCheck(e.Entity)))
        {
            numericColumns ??= await db.Database.SqlQueryRaw<NumericColumn>(
                "SELECT table_name AS \"TableName\", column_name AS \"ColumnName\", numeric_scale AS \"Scale\" FROM information_schema.columns WHERE table_schema = 'public' AND table_name IN ('payrolls', 'payroll_details', 'payroll_carry_forward_deductions', 'payroll_carry_forward_applications') AND data_type = 'numeric'").ToListAsync(ct);
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified && RequiresNumericCheck(e.Entity)))
            {
                var table = entry.Metadata.GetTableName()!;
                var identifier = StoreObjectIdentifier.Table(table, entry.Metadata.GetSchema());
                foreach (var property in entry.Properties.Where(p => p.CurrentValue is decimal))
                {
                    var column = property.Metadata.GetColumnName(identifier);
                    var scale = numericColumns.FirstOrDefault(c => c.TableName == table && c.ColumnName == column)?.Scale;
                    if (scale is >= 0 and < 28 && decimal.Round((decimal)property.CurrentValue!, scale.Value) != (decimal)property.CurrentValue!)
                        throw new InvalidOperationException($"Cột {table}.{column} chỉ lưu được {scale} chữ số thập phân. Cần đổi cột sang NUMERIC không giới hạn scale để giữ kết quả, hệ thống không tự làm tròn.");
                }
            }
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.ConstraintName != null
            && pg.ConstraintName.Contains("notifications", StringComparison.OrdinalIgnoreCase))
        {
            // If in-app notification violates a database constraint, detach notification entities and ensure the core payroll transaction succeeds
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.Entity is WorkShiftManagement.DAL.Entities.Notification or WorkShiftManagement.DAL.Entities.UserNotification).ToList())
            {
                entry.State = EntityState.Detached;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState is "23505" or "23503")
        { throw new InvalidOperationException("Dữ liệu nguồn đã được sử dụng hoặc thay đổi. Tải lại trước khi tính lương.", ex); }
    }
}
