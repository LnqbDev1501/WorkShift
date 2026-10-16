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
        var lockedId = await _context.EmployeeRequests
            .FromSqlInterpolated($"SELECT * FROM public.employee_requests WHERE request_id = {requestId} AND branch_id = {branchId} FOR UPDATE")
            .Select(r => (int?)r.RequestId).SingleOrDefaultAsync(cancellationToken);
        if (!lockedId.HasValue) throw new KeyNotFoundException("Request not found in this branch.");
        var request = await _context.EmployeeRequests.Include(r => r.User).ThenInclude(u => u.Role).Include(r => r.Branch)
            .Include(r => r.SalaryIncreaseRequestDetail).ThenInclude(d => d!.CurrentContract)
            .SingleAsync(r => r.RequestId == requestId, cancellationToken);
        SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)));
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
