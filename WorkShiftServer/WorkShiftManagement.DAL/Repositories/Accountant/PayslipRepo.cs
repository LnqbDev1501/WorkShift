using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayslipRepo(AppDbContext db) : IPayslipRepo
{
    private IQueryable<Payslip> Scoped(PayslipScope scope) => db.Payslips.AsNoTracking()
        .Where(s => (s.Status == "PUBLISHED" || s.Status == "REPLACED")
            && (!scope.BranchId.HasValue || s.Payroll.BranchId == scope.BranchId)
            && (!scope.UserId.HasValue || s.Payroll.UserId == scope.UserId));

    public async Task<(List<Payslip> Items, int Count)> ListAsync(PayslipScope scope, PayslipSearch q, CancellationToken ct)
    {
        var rows = Scoped(scope);
        if (q.Status != null) rows = rows.Where(s => s.Status == q.Status);
        else if (!q.IncludeHistory) rows = rows.Where(s => s.Status == "PUBLISHED");
        if (q.UserId.HasValue) rows = rows.Where(s => s.Payroll.UserId == q.UserId);
        if (q.BranchId.HasValue) rows = rows.Where(s => s.Payroll.BranchId == q.BranchId);
        if (q.PayrollPeriodId.HasValue) rows = rows.Where(s => s.Payroll.PayrollPeriodId == q.PayrollPeriodId);
        if (q.PeriodFrom.HasValue) rows = rows.Where(s => s.PeriodEndSnapshot >= q.PeriodFrom);
        if (q.PeriodTo.HasValue) rows = rows.Where(s => s.PeriodStartSnapshot <= q.PeriodTo);
        if (q.Keyword != null)
        {
            // Treat %, _ and backslash as literal characters, not SQL wildcards.
            var pattern = "%" + q.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(s => EF.Functions.ILike(s.PeriodCodeSnapshot, pattern, "\\")
                || EF.Functions.ILike(s.PeriodNameSnapshot, pattern, "\\")
                || EF.Functions.ILike(s.Payroll.EmployeeFullNameSnapshot, pattern, "\\"));
        }
        var count = await rows.CountAsync(ct);
        var items = await rows.Include(s => s.Payroll).ThenInclude(p => p.PayrollPeriod)
            .OrderByDescending(s => s.PublishedAt).ThenByDescending(s => s.PayslipId)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return (items, count);
    }

    public Task<Payslip?> GetAsync(PayslipScope scope, int id, CancellationToken ct) => Scoped(scope)
        .Include(s => s.Payroll).ThenInclude(p => p.PayrollPeriod)
        .Include(s => s.Payroll).ThenInclude(p => p.PayrollDetails)
        .Include(s => s.PayslipWorkDetails).AsSplitQuery().SingleOrDefaultAsync(s => s.PayslipId == id, ct);

    public Task<List<Payslip>> VersionsAsync(PayslipScope scope, Guid userId, int periodId, CancellationToken ct)
        => Scoped(scope).Include(s => s.Payroll).ThenInclude(p => p.PayrollPeriod)
            .Where(s => s.Payroll.UserId == userId && s.Payroll.PayrollPeriodId == periodId)
            .OrderByDescending(s => s.VersionNo).ThenByDescending(s => s.PayslipId).ToListAsync(ct);

    public async Task<HashSet<(Guid UserId, int PeriodId)>> UnresolvedAsync(IEnumerable<Payslip> slips, CancellationToken ct)
    {
        var list = slips.ToList();
        if (list.Count == 0) return [];
        var users = list.Select(s => s.Payroll.UserId).Distinct().ToList();
        var periods = list.Select(s => s.Payroll.PayrollPeriodId).Distinct().ToList();
        var pairs = await db.PayrollFeedbackRequests.AsNoTracking()
            .Where(f => users.Contains(f.EmployeeId) && periods.Contains(f.Payroll.PayrollPeriodId)
                && (f.Status == "SUBMITTED" || f.Status == "APPROVED" && f.CorrectionStatus != "COMPLETED"))
            .Select(f => new { f.EmployeeId, f.Payroll.PayrollPeriodId }).Distinct().ToListAsync(ct);
        return pairs.Select(p => (p.EmployeeId, p.PayrollPeriodId)).ToHashSet();
    }

    public Task<List<int>> DuePeriodIdsAsync(DateOnly periodEnd, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => p.Status == "APPROVED" && p.EndDate == periodEnd
            && (!db.Payslips.Any(s => s.Payroll.PayrollPeriodId == p.PayrollPeriodId)
                || db.Payrolls.Any(r => r.PayrollPeriodId == p.PayrollPeriodId && r.Status == "APPROVED"
                    && !db.Payrolls.Any(n => n.PayrollPeriodId == p.PayrollPeriodId && n.UserId == r.UserId
                        && n.Status == "APPROVED" && (n.VersionNo > r.VersionNo || n.VersionNo == r.VersionNo && n.PayrollId > r.PayrollId))
                    && !db.Payslips.Any(s => s.PayrollId == r.PayrollId))))
        .OrderBy(p => p.PayrollPeriodId).Select(p => p.PayrollPeriodId).ToListAsync(ct);

    public Task<List<Payroll>> PublicationPayrollsAsync(int periodId, CancellationToken ct) => db.Payrolls
        .Include(p => p.PayrollDetails).Include(p => p.Payslip)
        .Where(p => p.PayrollPeriodId == periodId).ToListAsync(ct);

    public async Task<HashSet<Guid>> ExpectedEmployeesAsync(PayrollPeriod period, CancellationToken ct)
    {
        var ids = await db.Timesheets.Where(t => t.PayrollPeriodId == period.PayrollPeriodId).Select(t => t.UserId)
            .Union(db.ShiftAssignments.Where(a => a.Shift.ScheduleWeekId == period.ScheduleWeekId
                && a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED").Select(a => a.UserId)).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public void Add(Payslip slip) => db.Payslips.Add(slip);
    public void RecordPublicationAudit(int periodId, DateTime scheduledAt, DateTime publishedAt, int count)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = "PUBLISH_INITIAL_PAYSLIPS", TableName = "payroll_periods", RecordId = periodId.ToString(),
            CreatedAt = publishedAt, NewValue = JsonSerializer.Serialize(new { scheduledAt, publishedAt, payslipCount = count })
        });
    }
}
