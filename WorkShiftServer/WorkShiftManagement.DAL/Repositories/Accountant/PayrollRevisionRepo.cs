using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.DAL.Repositories.Accountant;
public sealed class PayrollRevisionRepo(AppDbContext db) : IPayrollRevisionRepo
{
    private IQueryable<Payroll> Graph => db.Payrolls.Include(p => p.PayrollDetails).Include(p => p.PreviousPayroll)
        .ThenInclude(p => p!.PayrollDetails).Include(p => p.Timesheet).ThenInclude(t => t.User).Include(p => p.PayrollPeriod);
    public Task<Payroll?> GetAsync(int id, int? branchId, CancellationToken ct) => Graph.SingleOrDefaultAsync(
        p => p.PayrollId == id && (!branchId.HasValue || p.BranchId == branchId), ct);
    public Task<List<Payroll>> VersionsAsync(Guid userId, int periodId, CancellationToken ct) => Graph
        .Where(p => p.UserId == userId && p.PayrollPeriodId == periodId).OrderBy(p => p.VersionNo).ThenBy(p => p.PayrollId).ToListAsync(ct);
    public Task<PayrollFeedbackRequest?> FeedbackAsync(int id, CancellationToken ct) => db.PayrollFeedbackRequests
        .Include(f => f.Payroll).Include(f => f.Payslip).SingleOrDefaultAsync(f => f.FeedbackId == id, ct);
    public Task<PayrollFeedbackRequest?> CorrectionFeedbackAsync(int payrollId, CancellationToken ct) => db.PayrollFeedbackRequests
        .Include(f => f.Payroll).Include(f => f.Payslip).SingleOrDefaultAsync(f => f.ResolvedPayrollId == payrollId
            && f.Status == "APPROVED" && f.CorrectionStatus == "IN_PROGRESS", ct);
    public Task<Payslip?> PublishedAsync(Guid userId, int periodId, CancellationToken ct) => db.Payslips
        .Include(p => p.Payroll).SingleOrDefaultAsync(p => p.Payroll.UserId == userId
            && p.Payroll.PayrollPeriodId == periodId && p.Status == "PUBLISHED", ct);
    public void Add(Payslip payslip) => db.Payslips.Add(payslip);
    public void Audit(Guid actor, string action, Payroll payroll) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actor, Action = action, TableName = "payrolls", RecordId = payroll.PayrollId.ToString(), CreatedAt = DateTime.UtcNow,
        NewValue = JsonSerializer.Serialize(new { payroll.PayrollId, payroll.PreviousPayrollId, payroll.VersionNo,
            payroll.CorrectionReason, payroll.Status, payroll.NetSalary, payroll.ApprovedBy, payroll.SubmittedBy })
    });
}
