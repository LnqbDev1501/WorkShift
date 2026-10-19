using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayrollSettlementRepo(AppDbContext db) : IPayrollSettlementRepo
{
    private IQueryable<PayrollCarryForwardDeduction> CarryGraph(bool tracking)
    {
        IQueryable<PayrollCarryForwardDeduction> query = db.PayrollCarryForwardDeductions;
        if (!tracking) query = query.AsNoTracking();
        return query.Include(c => c.SourcePayroll).ThenInclude(p => p.PayrollPeriod).Include(c => c.SourcePayrollDetail)
            .Include(c => c.PayrollCarryForwardApplications).ThenInclude(a => a.Payroll).ThenInclude(p => p.PayrollPeriod)
            .Include(c => c.PayrollCarryForwardApplications).ThenInclude(a => a.Payroll).ThenInclude(p => p.PayrollDetails).AsSplitQuery();
    }
    public async Task<PayrollSettlementData?> LoadAsync(int periodId, int? branchId, bool tracking, CancellationToken ct)
    {
        var periods = tracking ? db.PayrollPeriods.AsQueryable() : db.PayrollPeriods.AsNoTracking();
        var period = await periods.SingleOrDefaultAsync(p => p.PayrollPeriodId == periodId && (!branchId.HasValue || p.BranchId == branchId), ct);
        if (period == null) return null;
        var payrolls = tracking ? db.Payrolls.AsQueryable() : db.Payrolls.AsNoTracking();
        var rows = await payrolls.Where(p => p.PayrollPeriodId == periodId).Include(p => p.PayrollPeriod)
            .Include(p => p.PayrollDetails).Include(p => p.Payslip).AsSplitQuery().ToListAsync(ct);
        var expected = await db.Timesheets.Where(t => t.PayrollPeriodId == periodId).Select(t => t.UserId)
            .Union(db.ShiftAssignments.Where(a => a.Shift.ScheduleWeekId == period.ScheduleWeekId
                && a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED").Select(a => a.UserId)).ToListAsync(ct);
        var users = expected.Union(rows.Select(p => p.UserId)).ToList();
        var prior = await db.Timesheets.Where(t => users.Contains(t.UserId) && t.PayrollPeriod.StartDate < period.StartDate
            && t.PayrollPeriod.Status != "PAID" && t.PayrollPeriod.Status != "CLOSED").Select(t => t.UserId)
            .Union(db.ShiftAssignments.Where(a => users.Contains(a.UserId) && a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED"
                && db.PayrollPeriods.Any(p => p.ScheduleWeekId == a.Shift.ScheduleWeekId && p.StartDate < period.StartDate
                    && p.Status != "PAID" && p.Status != "CLOSED")).Select(a => a.UserId)).ToListAsync(ct);
        return new()
        {
            Period = period, Payrolls = rows, ExpectedEmployees = expected.ToHashSet(), UnsettledPriorEmployees = prior.ToHashSet(),
            UnresolvedFeedbackCount = await db.PayrollFeedbackRequests.CountAsync(f => f.Payroll.PayrollPeriodId == periodId
                && (f.ReviewStatus == "PENDING" || f.ReviewStatus == "FEEDBACK_PENDING"), ct)
                + await db.PayrollCorrectionRequests.CountAsync(r => r.Payroll.PayrollPeriodId == periodId && (r.Status == "OPEN" || r.Status == "RESPONDED"), ct),
            CarryForwards = await CarryGraph(tracking).Where(c => users.Contains(c.UserId)).ToListAsync(ct)
        };
    }
    public async Task<(List<PayrollCarryForwardDeduction> Items, int Count)> ListCarryAsync(int branchId, CarryForwardSearch q, CancellationToken ct)
    {
        var rows = db.PayrollCarryForwardDeductions.AsNoTracking().Where(c => c.SourcePayroll.BranchId == branchId);
        if (q.UserId.HasValue) rows = rows.Where(c => c.UserId == q.UserId);
        if (q.Status != null) rows = rows.Where(c => c.Status == q.Status);
        if (q.SourcePayrollId.HasValue) rows = rows.Where(c => c.SourcePayrollId == q.SourcePayrollId);
        if (q.SourcePayrollPeriodId.HasValue) rows = rows.Where(c => c.SourcePayroll.PayrollPeriodId == q.SourcePayrollPeriodId);
        if (q.CreatedFromUtc.HasValue) rows = rows.Where(c => c.CreatedAt >= q.CreatedFromUtc);
        if (q.CreatedToExclusiveUtc.HasValue) rows = rows.Where(c => c.CreatedAt < q.CreatedToExclusiveUtc);
        if (q.Keyword != null)
        {
            var pattern = "%" + q.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var numeric = int.TryParse(q.Keyword, out var id);
            rows = rows.Where(c => EF.Functions.ILike(c.SourcePayroll.EmployeeFullNameSnapshot, pattern, "\\")
                || EF.Functions.ILike(c.Reason, pattern, "\\") || numeric && c.CarryForwardId == id);
        }
        var count = await rows.CountAsync(ct);
        var ids = await rows.OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).Select(c => c.CarryForwardId).ToListAsync(ct);
        return (await CarryGraph(false).Where(c => ids.Contains(c.CarryForwardId)).OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId).ToListAsync(ct), count);
    }
    public Task<PayrollCarryForwardDeduction?> GetCarryAsync(int id, int branchId, CancellationToken ct) => CarryGraph(false)
        .SingleOrDefaultAsync(c => c.CarryForwardId == id && c.SourcePayroll.BranchId == branchId, ct);
    public void Add(PayrollCarryForwardDeduction carry) => db.PayrollCarryForwardDeductions.Add(carry);
    public void Add(PayrollCarryForwardApplication application) => db.PayrollCarryForwardApplications.Add(application);
    public void Audit(Guid? actor, string action, PayrollPeriod period, DateTime now, object payload) => db.AuditLogs.Add(new AuditLog
    { UserId = actor, Action = action, TableName = "payroll_periods", RecordId = period.PayrollPeriodId.ToString(), CreatedAt = now, NewValue = JsonSerializer.Serialize(payload) });
    public Task<List<int>> PendingClosureIdsAsync(CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => p.Status == "CLOSED" && db.AuditLogs.Any(a => a.Action == "AUTO_CLOSE_PAYROLL_PERIOD" && a.RecordId == p.PayrollPeriodId.ToString())
            && !db.AuditLogs.Any(a => a.Action == "PAYROLL_CLOSURE_NOTIFIED" && a.RecordId == p.PayrollPeriodId.ToString()))
        .OrderBy(p => p.ClosedAt).Take(100).Select(p => p.PayrollPeriodId).ToListAsync(ct);
    public async Task<long?> PendingClosureEventAsync(int periodId, CancellationToken ct)
    {
        var record = periodId.ToString();
        if (await db.AuditLogs.AnyAsync(a => a.Action == "PAYROLL_CLOSURE_NOTIFIED" && a.RecordId == record, ct)) return null;
        return await db.AuditLogs.Where(a => a.Action == "AUTO_CLOSE_PAYROLL_PERIOD" && a.RecordId == record)
            .OrderBy(a => a.AuditLogId).Select(a => (long?)a.AuditLogId).FirstOrDefaultAsync(ct);
    }
}
