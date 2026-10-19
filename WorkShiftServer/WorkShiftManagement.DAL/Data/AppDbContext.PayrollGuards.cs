using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Version.Payroll;
namespace WorkShiftManagement.DAL.Data;

public partial class AppDbContext
{
    // Enforce the freeze for all EF repository writers, not just Payroll controllers.
    // Snapshot/history reads remain unchanged; future salary records may still be inserted.
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(true, cancellationToken);
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var entries = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && e.Entity is Timesheet or Attendance or ShiftAssignment or WorkShift or AttendancePenaltyRecord or StaffSalaryHistory
                or EmployeeRewardsPenalty or HolidayPayRule or PublicHolidayConfig or PayrollDetail or Payroll).ToList();
        if (entries.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        IDbContextTransaction? owned = null;
        try
        {
            if (Database.IsNpgsql())
            {
                if (Database.CurrentTransaction == null) owned = await Database.BeginTransactionAsync(cancellationToken);
                await Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", cancellationToken);
            }
            await ValidatePayrollFreeze(entries, cancellationToken);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (owned != null) await owned.CommitAsync(cancellationToken);
            return result;
        }
        finally { if (owned != null) await owned.DisposeAsync(); }
    }
    private async Task ValidatePayrollFreeze(List<EntityEntry> entries, CancellationToken ct)
    {
        var frozen = PayrollPeriods.AsNoTracking().Where(p => p.Status == "SUBMITTED" || p.Status == "APPROVED" || p.Status == "PAID" || p.Status == "CLOSED");
        var frozenPayrolls = Payrolls.AsNoTracking().Where(p => frozen.Any(f => f.PayrollPeriodId == p.PayrollPeriodId));
        static bool Changes(EntityEntry e, params string[] fields) => e.State == EntityState.Deleted || e.Properties.Any(p => p.IsModified && fields.Contains(p.Metadata.Name));
        static IEnumerable<T> Keys<T>(EntityEntry e, string property)
        {
            yield return (T)e.Property(property).CurrentValue!;
            if (e.State != EntityState.Added) yield return (T)e.Property(property).OriginalValue!;
        }
        foreach (var e in entries)
        {
            bool blocked = false;
            switch (e.Entity)
            {
                case Payroll p when e.State != EntityState.Added:
                    var allowed = new[] { "Status", "PaymentConfirmedBy", "PaymentConfirmedAt", "PaidAt", "PaymentReference", "PaymentNote", "UpdatedAt" };
                    blocked = e.State == EntityState.Deleted || e.Properties.Any(x => x.IsModified && !allowed.Contains(x.Metadata.Name));
                    blocked = blocked && await frozenPayrolls.AnyAsync(x => x.PayrollId == p.PayrollId, ct);
                    if (p.Status == "APPROVED" && (string)e.Property(nameof(p.Status)).OriginalValue! == "SUBMITTED"
                        && PayrollInputFreezePolicy.IsFinalApproval(ChangeTracker, e, p.PayrollPeriodId, "Status", "ApprovedBy", "ApprovedAt", "UpdatedAt")) blocked = false;
                    break;
                case PayrollDetail detail:
                    var payrollIds = Keys<int>(e, nameof(detail.PayrollId)).ToArray();
                    blocked = await frozenPayrolls.AnyAsync(p => payrollIds.Contains(p.PayrollId), ct);
                    break;
                case Timesheet sheet:
                    var periodIds = Keys<int>(e, nameof(sheet.PayrollPeriodId)).ToArray();
                    blocked = await frozen.AnyAsync(p => periodIds.Contains(p.PayrollPeriodId), ct);
                    var approvalLock = sheet.Status == "LOCKED" && PayrollInputFreezePolicy.IsFinalApproval(ChangeTracker, e,
                        sheet.PayrollPeriodId, "Status", "LockedAt", "LockedBy", "UpdatedAt");
                    if (approvalLock) blocked = false;
                    break;
                case WorkShift shift:
                    var weeks = Keys<int>(e, nameof(shift.ScheduleWeekId)).ToArray();
                    blocked = await frozen.AnyAsync(p => p.ScheduleWeekId.HasValue && weeks.Contains(p.ScheduleWeekId.Value), ct);
                    break;
                case ShiftAssignment assignment:
                    var shifts = Keys<int>(e, nameof(assignment.ShiftId)).ToArray();
                    blocked = await WorkShifts.AnyAsync(s => shifts.Contains(s.ShiftId) && frozen.Any(p => p.ScheduleWeekId == s.ScheduleWeekId), ct);
                    break;
                case Attendance attendance:
                    var assignments = Keys<int>(e, nameof(attendance.AssignmentId)).ToArray();
                    blocked = await ShiftAssignments.AnyAsync(a => assignments.Contains(a.AssignmentId) && frozen.Any(p => p.ScheduleWeekId == a.Shift.ScheduleWeekId), ct);
                    break;
                case AttendancePenaltyRecord penalty:
                    var attendanceIds = Keys<int>(e, nameof(penalty.AttendanceId)).ToArray();
                    blocked = await Attendances.AnyAsync(a => attendanceIds.Contains(a.AttendanceId) && frozen.Any(p => p.ScheduleWeekId == a.Assignment.Shift.ScheduleWeekId), ct);
                    break;
                case EmployeeRewardsPenalty adjustment:
                    var rewardPeriods = Keys<int>(e, nameof(adjustment.PayrollPeriodId)).ToArray();
                    blocked = await frozen.AnyAsync(p => rewardPeriods.Contains(p.PayrollPeriodId), ct);
                    break;
                case StaffSalaryHistory salary when e.State != EntityState.Added:
                    // A rate change always creates a new history. Never edit the old rate in place.
                    if (Changes(e, "HourlyRate", "PreviousHourlyRate", "EffectiveFrom", "UserId", "ContractId"))
                        throw new InvalidOperationException("Không ghi đè đơn giá/lịch sử lương cũ. Phải tạo lịch sử mức lương mới qua nghiệp vụ được duyệt.");
                    if (Changes(e, "EffectiveTo") && salary.EffectiveTo.HasValue)
                        blocked = await PayrollDetails.AnyAsync(d => d.SalaryHistoryId == salary.SalaryHistoryId && frozenPayrolls.Any(p => p.PayrollId == d.PayrollId)
                            && d.WorkTo > salary.EffectiveTo, ct);
                    break;
                case PublicHolidayConfig holiday when e.State != EntityState.Added:
                    blocked = await PayrollDetails.AnyAsync(d => d.PublicHolidayId == holiday.PublicHolidayId && frozenPayrolls.Any(p => p.PayrollId == d.PayrollId), ct);
                    break;
                case HolidayPayRule rule when e.State != EntityState.Added:
                    // Deactivation alone can stop future use while snapshots preserve historical multipliers.
                    if (Changes(e, "PayMultiplier", "EffectiveFrom", "EffectiveTo"))
                        blocked = await PayrollDetails.AnyAsync(d => d.HolidayPayRuleId == rule.HolidayPayRuleId && frozenPayrolls.Any(p => p.PayrollId == d.PayrollId), ct);
                    break;
            }
            if (blocked) throw new InvalidOperationException("Dữ liệu đầu vào/chi tiết lương đã đóng băng từ khi Submit kỳ; không được sửa kỳ đã gửi duyệt hoặc đã hoàn tất.");
        }
    }
}
