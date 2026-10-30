using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Repositories.Staff;
namespace WorkShiftManagement.DAL.Repositories.Manager;

public partial class ManagerEmployeeRequestRepo
{
    private async Task<EmployeeRequest> ApproveSalaryIncreaseRequestAsync(int requestId, int branchId, Guid managerId,
        string? reviewNote, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        // Keep the salary/contract source stable throughout approval. Manager acceptance
        // must not validate a stale snapshot while an Admin changes the current rate.
        await _context.Database.ExecuteSqlRawAsync(
            "LOCK TABLE public.staff_salary_history, public.employment_contracts IN SHARE MODE", cancellationToken);
        var lockedId = await _context.EmployeeRequests
            .FromSqlInterpolated($"SELECT * FROM public.employee_requests WHERE request_id = {requestId} AND branch_id = {branchId} FOR UPDATE")
            .Select(r => (int?)r.RequestId).SingleOrDefaultAsync(cancellationToken);
        if (!lockedId.HasValue) throw new KeyNotFoundException("Request not found in this branch.");
        var request = await _context.EmployeeRequests.Include(r => r.User).ThenInclude(u => u.Role)
            .Include(r => r.User).ThenInclude(u => u.Branch).Include(r => r.Branch)
            .Include(r => r.SalaryIncreaseRequestDetail).ThenInclude(d => d!.CurrentContract)
            .SingleAsync(r => r.RequestId == requestId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var salaries = await _context.StaffSalaryHistories.AsNoTracking().Include(s => s.Contract)
            .Where(s => s.UserId == request.UserId && s.EffectiveFrom <= today
                && (!s.EffectiveTo.HasValue || s.EffectiveTo >= today)).ToListAsync(cancellationToken);
        SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, salaries);
        var oldValue = JsonSerializer.Serialize(new { request.Status, request.ReviewedBy, request.ReviewedAt, request.ReviewNote });
        var now = DateTime.UtcNow;
        request.Status = "APPROVED"; request.ReviewedBy = managerId; request.ReviewedAt = now;
        request.ReviewNote = reviewNote?.Trim(); request.UpdatedAt = now;
        _context.AuditLogs.Add(new AuditLog { UserId = managerId, Action = "APPROVE", TableName = "employee_requests",
            RecordId = requestId.ToString(), OldValue = oldValue, CreatedAt = now,
            NewValue = JsonSerializer.Serialize(new { request.Status, request.ReviewedBy, request.ReviewedAt, request.ReviewNote,
                request.RequestType, request.SalaryIncreaseRequestDetail!.CurrentContractId,
                request.SalaryIncreaseRequestDetail.CurrentHourlyRate, request.SalaryIncreaseRequestDetail.ProposedHourlyRate,
                salaryChanged = false }) });
        // Manager accepts the Staff's request only. Admin proposal/approval and salary activation are separate work.
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request;
    }
}
