using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.DAL.Repositories.Accountant;
public sealed class PayrollFeedbackRepo(AppDbContext db) : IPayrollFeedbackRepo
{
    public async Task<PayrollReviewData?> LoadAsync(int periodId, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.SingleOrDefaultAsync(p => p.PayrollPeriodId == periodId, ct);
        if (period == null) return null;
        return new() {
            Period = period,
            Payrolls = await db.Payrolls.Where(p => p.PayrollPeriodId == periodId)
                .Include(p => p.PayrollPeriod).Include(p => p.PayrollDetails).Include(p => p.Payslip)
                .Include(p => p.Timesheet).ThenInclude(t => t.User).AsSplitQuery().ToListAsync(ct),
            Reviews = await db.PayrollFeedbackRequests.Where(f => f.Payroll.PayrollPeriodId == periodId)
                .Include(f => f.Payroll).OrderBy(f => f.FeedbackId).ToListAsync(ct),
            Corrections = await db.PayrollCorrectionRequests.Where(r => r.Payroll.PayrollPeriodId == periodId)
                .Include(r => r.Payroll).OrderBy(r => r.CorrectionRequestId).ToListAsync(ct)
        };
    }
    public Task<int?> PeriodForReviewAsync(int id, CancellationToken ct) => db.PayrollFeedbackRequests.Where(f => f.FeedbackId == id).Select(f => (int?)f.Payroll.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<int?> PeriodForCorrectionAsync(int id, CancellationToken ct) => db.PayrollCorrectionRequests.Where(f => f.CorrectionRequestId == id).Select(f => (int?)f.Payroll.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<int?> PeriodForPayrollAsync(int id, CancellationToken ct) => db.Payrolls.Where(p => p.PayrollId == id).Select(p => (int?)p.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<List<int>> ReviewPeriodIdsAsync(int? branch, Guid? employee, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => (!branch.HasValue || p.BranchId == branch) && (!employee.HasValue || p.Payrolls.Any(w => w.UserId == employee && w.PayrollFeedbackRequests.Any())))
        .OrderByDescending(p => p.StartDate).Select(p => p.PayrollPeriodId).ToListAsync(ct);
    public Task<List<int>> DueReviewPeriodsAsync(DateTime now, CancellationToken ct) => db.PayrollFeedbackRequests.AsNoTracking()
        .Where(f => f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt <= now)
        .Select(f => f.Payroll.PayrollPeriodId).Distinct().ToListAsync(ct);
    public void Add(PayrollFeedbackRequest row) => db.PayrollFeedbackRequests.Add(row);
    public void Add(PayrollCorrectionRequest row) => db.PayrollCorrectionRequests.Add(row);
    public void Add(Payslip row) => db.Payslips.Add(row);
    public void Audit(Guid? actor, string action, string table, int id, object payload, DateTime now) => db.AuditLogs.Add(new AuditLog {
        UserId = actor, Action = action, TableName = table, RecordId = id.ToString(), NewValue = JsonSerializer.Serialize(payload), CreatedAt = now
    });
}
