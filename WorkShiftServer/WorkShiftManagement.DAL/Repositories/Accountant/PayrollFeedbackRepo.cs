using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayrollFeedbackRepo(AppDbContext db) : IPayrollFeedbackRepo
{
    private IQueryable<PayrollFeedbackRequest> Scoped(PayrollFeedbackScope scope) => db.PayrollFeedbackRequests
        .Where(f => (!scope.BranchId.HasValue || f.Payroll.BranchId == scope.BranchId)
            && (!scope.EmployeeId.HasValue || f.EmployeeId == scope.EmployeeId));

    private static IQueryable<PayrollFeedbackRequest> Graph(IQueryable<PayrollFeedbackRequest> query) => query
        .Include(f => f.Payroll).ThenInclude(p => p.PayrollPeriod).Include(f => f.Payslip)
        .Include(f => f.ResolvedPayroll).ThenInclude(p => p!.Payslip);

    public Task<Payslip?> GetPayslipAsync(int id, PayrollFeedbackScope scope, CancellationToken ct)
        => db.Payslips.Include(s => s.Payroll).ThenInclude(p => p.PayrollPeriod).SingleOrDefaultAsync(s => s.PayslipId == id
            && (!scope.BranchId.HasValue || s.Payroll.BranchId == scope.BranchId)
            && (!scope.EmployeeId.HasValue || s.Payroll.UserId == scope.EmployeeId), ct);

    public Task<bool> IsCurrentPayslipAsync(Payslip slip, CancellationToken ct) => db.Payslips.AnyAsync(s => s.PayslipId == slip.PayslipId
        && s.Status == "PUBLISHED" && !db.Payslips.Any(n => n.Payroll.UserId == s.Payroll.UserId
            && n.Payroll.PayrollPeriodId == s.Payroll.PayrollPeriodId && n.Status == "PUBLISHED"
            && (n.VersionNo > s.VersionNo || n.VersionNo == s.VersionNo && n.PayslipId > s.PayslipId)), ct);

    public Task<bool> HasUnresolvedAsync(Guid userId, int periodId, CancellationToken ct) => db.PayrollFeedbackRequests
        .AnyAsync(f => f.EmployeeId == userId && f.Payroll.PayrollPeriodId == periodId
            && (f.Status == "SUBMITTED" || f.Status == "APPROVED" && f.CorrectionStatus != "COMPLETED"), ct);

    public Task<PayrollFeedbackRequest?> GetAsync(int id, PayrollFeedbackScope scope, bool tracking, CancellationToken ct)
    {
        var query = Scoped(scope);
        if (!tracking) query = query.AsNoTracking();
        return Graph(query).Include(f => f.Payroll).ThenInclude(p => p.PayrollDetails)
            .Include(f => f.ResolvedPayroll).ThenInclude(p => p!.PayrollDetails)
            .AsSplitQuery().SingleOrDefaultAsync(f => f.FeedbackId == id, ct);
    }

    public async Task<(List<PayrollFeedbackRequest> Items, int Count)> ListAsync(PayrollFeedbackScope scope, PayrollFeedbackSearch q, CancellationToken ct)
    {
        var query = Scoped(scope).AsNoTracking();
        if (q.UserId.HasValue) query = query.Where(f => f.EmployeeId == q.UserId);
        if (q.PayrollPeriodId.HasValue) query = query.Where(f => f.Payroll.PayrollPeriodId == q.PayrollPeriodId);
        if (q.PayslipId.HasValue) query = query.Where(f => f.PayslipId == q.PayslipId);
        if (q.Status != null) query = query.Where(f => f.Status == q.Status);
        if (q.CorrectionStatus != null) query = query.Where(f => f.CorrectionStatus == q.CorrectionStatus);
        if (q.ReasonCode != null) query = query.Where(f => f.ReasonCode == q.ReasonCode);
        if (q.CreatedFromUtc.HasValue) query = query.Where(f => f.CreatedAt >= q.CreatedFromUtc);
        if (q.CreatedToExclusiveUtc.HasValue) query = query.Where(f => f.CreatedAt < q.CreatedToExclusiveUtc);
        if (q.Keyword != null)
        {
            var pattern = "%" + q.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var numericId = int.TryParse(q.Keyword, out var id);
            query = query.Where(f => EF.Functions.ILike(f.Payroll.EmployeeFullNameSnapshot, pattern, "\\")
                || EF.Functions.ILike(f.Description, pattern, "\\")
                || EF.Functions.ILike(f.Payslip.PeriodCodeSnapshot, pattern, "\\")
                || EF.Functions.ILike(f.Payslip.PeriodNameSnapshot, pattern, "\\")
                || numericId && f.FeedbackId == id);
        }
        var count = await query.CountAsync(ct);
        var items = await Graph(query).OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.FeedbackId)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return (items, count);
    }

    public void Add(PayrollFeedbackRequest feedback) => db.PayrollFeedbackRequests.Add(feedback);
    public void Audit(Guid actor, string action, PayrollFeedbackRequest feedback, DateTime now) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor, Action = action, TableName = "payroll_feedback_requests", RecordId = feedback.FeedbackId.ToString(), CreatedAt = now,
        NewValue = JsonSerializer.Serialize(new { feedback.PayrollId, feedback.PayslipId, feedback.EmployeeId, feedback.ReasonCode,
            feedback.Status, feedback.CorrectionStatus, feedback.ProcessedBy, feedback.ResolutionNote, feedback.ResolvedPayrollId })
    });
}
