using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public partial class ManagerEmployeeRequestRepo : IManagerEmployeeRequestRepo
{
    private readonly AppDbContext _context;

    public ManagerEmployeeRequestRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.UserId == managerId && u.DeletedAt == null && u.Status == "ACTIVE", cancellationToken);
    }

    public async Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        int branchId,
        string? search,
        string? requestType,
        string? status,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeRequests.AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.User)
            .Include(r => r.Branch)
            .Include(r => r.SalaryIncreaseRequestDetail).ThenInclude(d => d!.CurrentContract)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
            .Where(r => r.BranchId == branchId &&
                        (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"));

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(r => r.RequestType == requestType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt < toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(r => EF.Functions.ILike(r.User.FullName, $"%{trimmedSearch}%") ||
                                     (r.Reason != null && EF.Functions.ILike(r.Reason, $"%{trimmedSearch}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmployeeRequest?> GetByIdAsync(
        int requestId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeRequests.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Branch)
            .Include(r => r.SalaryIncreaseRequestDetail).ThenInclude(d => d!.CurrentContract)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.AppliedRequestLimitRule)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ScheduleWeek)
                            .ThenInclude(w => w.Branch)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ScheduleWeek)
                            .ThenInclude(w => w.Branch)
            .FirstOrDefaultAsync(r => r.RequestId == requestId &&
                                      r.BranchId == branchId &&
                                      (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"),
                                 cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetHistoryAsync(
        int requestId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.EmployeeRequests.AsNoTracking()
            .AnyAsync(r => r.RequestId == requestId &&
                           r.BranchId == branchId &&
                           (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"),
                      cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException("Request not found in this branch.");
        }

        var recordIdStr = requestId.ToString();
        return await _context.AuditLogs.AsNoTracking()
            .Include(a => a.User)
                .ThenInclude(u => u!.Role)
            .Where(a => a.TableName == "employee_requests" && a.RecordId == recordIdStr)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.AuditLogId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeRequest> ApproveRequestAsync(
        int requestId,
        int branchId,
        Guid managerId,
        string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        if (await _context.EmployeeRequests.AsNoTracking().AnyAsync(r => r.RequestId == requestId && r.BranchId == branchId && r.RequestType == "SALARY_INCREASE", cancellationToken))
            return await ApproveSalaryIncreaseRequestAsync(requestId, branchId, managerId, reviewNote, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // Acquire row lock to prevent race conditions
            var lockedId = await _context.EmployeeRequests
                .FromSqlInterpolated($"SELECT * FROM public.employee_requests WHERE request_id = {requestId} AND branch_id = {branchId} FOR UPDATE")
                .Select(r => (int?)r.RequestId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!lockedId.HasValue)
            {
                throw new KeyNotFoundException("Request not found in this branch.");
            }

            var request = await _context.EmployeeRequests
                .Include(r => r.User)
                .Include(r => r.Branch)
                .Include(r => r.LeaveRequestAssignments)
                    .ThenInclude(lra => lra.Assignment)
                        .ThenInclude(a => a.Shift)
                            .ThenInclude(s => s.ScheduleWeek)
                .Include(r => r.TimeAdjustmentRequestDetail)
                    .ThenInclude(tard => tard!.Assignment)
                        .ThenInclude(a => a.Shift)
                            .ThenInclude(s => s.ScheduleWeek)
                .SingleAsync(r => r.RequestId == requestId, cancellationToken);

            if (request.RequestType != "LEAVE" && request.RequestType != "LATE_ARRIVAL" && request.RequestType != "EARLY_LEAVE")
            {
                throw new KeyNotFoundException("Request type is not supported.");
            }

            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"Request is already {request.Status} and cannot be approved.");
            }

            // Collect and lock all relevant assignment IDs in strictly ascending order
            var assignmentIds = request.RequestType == "LEAVE"
                ? request.LeaveRequestAssignments.Select(lra => lra.AssignmentId).OrderBy(id => id).ToList()
                : (request.TimeAdjustmentRequestDetail != null ? new List<int> { request.TimeAdjustmentRequestDetail.AssignmentId } : new List<int>());

            if (assignmentIds.Count == 0)
            {
                throw new InvalidOperationException("Request contains no shift assignments to lock.");
            }

            foreach (var aid in assignmentIds)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT assignment_id FROM public.shift_assignments WHERE assignment_id = {aid} FOR UPDATE",
                    cancellationToken);
            }

            var now = DateTime.UtcNow;

            // Validate assignments belong to staff, branch, shift status is SCHEDULED, assignment status is ASSIGNED, shift has not ended, and no conflicting requests
            if (request.RequestType == "LEAVE")
            {
                foreach (var lra in request.LeaveRequestAssignments)
                {
                    if (lra.Assignment.UserId != request.UserId)
                    {
                        throw new InvalidOperationException("Assignment does not belong to the requesting employee.");
                    }
                    if (lra.Assignment.Shift.ScheduleWeek.BranchId != branchId)
                    {
                        throw new InvalidOperationException("Assignment shift does not belong to the manager's branch.");
                    }
                    if (lra.Assignment.Status != "ASSIGNED")
                    {
                        throw new InvalidOperationException($"Assignment #{lra.AssignmentId} is in status '{lra.Assignment.Status}'; only 'ASSIGNED' assignments can have leave approved.");
                    }
                    if (lra.Assignment.Shift.Status != "SCHEDULED")
                    {
                        throw new InvalidOperationException($"Shift #{lra.Assignment.ShiftId} is in status '{lra.Assignment.Shift.Status}'; only 'SCHEDULED' shifts can have requests approved.");
                    }
                    if (now >= lra.Assignment.Shift.ScheduledEndAt)
                    {
                        throw new InvalidOperationException($"Cannot approve leave request: shift #{lra.Assignment.ShiftId} has already ended at {lra.Assignment.Shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss} UTC.");
                    }

                    // Check conflicting approved leave
                    var hasLeaveConflict = await _context.LeaveRequestAssignments
                        .AnyAsync(x => x.AssignmentId == lra.AssignmentId &&
                                       x.RequestId != requestId &&
                                       x.Request.Status == "APPROVED",
                                  cancellationToken);
                    if (hasLeaveConflict)
                    {
                        throw new InvalidOperationException($"An approved leave request already exists for assignment #{lra.AssignmentId}.");
                    }

                    // Check cross-conflict with approved time adjustment requests (LATE_ARRIVAL / EARLY_LEAVE)
                    var hasTimeAdjustmentConflict = await _context.TimeAdjustmentRequestDetails
                        .AnyAsync(x => x.AssignmentId == lra.AssignmentId &&
                                       x.Request.Status == "APPROVED",
                                  cancellationToken);
                    if (hasTimeAdjustmentConflict)
                    {
                        throw new InvalidOperationException($"Cannot approve leave request: an approved time adjustment request already exists for assignment #{lra.AssignmentId}.");
                    }
                }
            }
            else if (request.RequestType is "LATE_ARRIVAL" or "EARLY_LEAVE")
            {
                var tard = request.TimeAdjustmentRequestDetail
                    ?? throw new InvalidOperationException("Time adjustment detail not found for this request.");

                if (tard.Assignment.UserId != request.UserId)
                {
                    throw new InvalidOperationException("Assignment does not belong to the requesting employee.");
                }
                if (tard.Assignment.Shift.ScheduleWeek.BranchId != branchId)
                {
                    throw new InvalidOperationException("Assignment shift does not belong to the manager's branch.");
                }
                if (tard.Assignment.Status != "ASSIGNED")
                {
                    throw new InvalidOperationException($"Assignment #{tard.AssignmentId} is in status '{tard.Assignment.Status}'; only 'ASSIGNED' assignments can have time adjustments approved.");
                }
                if (tard.Assignment.Shift.Status != "SCHEDULED")
                {
                    throw new InvalidOperationException($"Shift #{tard.Assignment.Shift.ShiftId} is in status '{tard.Assignment.Shift.Status}'; only 'SCHEDULED' shifts can have requests approved.");
                }
                if (now >= tard.Assignment.Shift.ScheduledEndAt)
                {
                    throw new InvalidOperationException($"Cannot approve time adjustment request: shift #{tard.Assignment.Shift.ShiftId} has already ended at {tard.Assignment.Shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss} UTC.");
                }

                // Check cross-conflict with approved LEAVE
                var hasLeaveConflict = await _context.LeaveRequestAssignments
                    .AnyAsync(x => x.AssignmentId == tard.AssignmentId &&
                                   x.Request.Status == "APPROVED",
                              cancellationToken);
                if (hasLeaveConflict)
                {
                    throw new InvalidOperationException($"Cannot approve time adjustment: an approved leave request already exists for assignment #{tard.AssignmentId}.");
                }

                var shift = tard.Assignment.Shift;
                if (request.RequestType == "LATE_ARRIVAL")
                {
                    if (tard.RequestedTime <= shift.ScheduledStartAt || tard.RequestedTime >= shift.ScheduledEndAt)
                    {
                        throw new InvalidOperationException($"Requested late arrival time ({tard.RequestedTime:yyyy-MM-dd HH:mm:ss}) must be between shift start ({shift.ScheduledStartAt:yyyy-MM-dd HH:mm:ss}) and shift end ({shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss}).");
                    }

                    var hasConflict = await _context.TimeAdjustmentRequestDetails
                        .AnyAsync(x => x.AssignmentId == tard.AssignmentId &&
                                       x.RequestId != requestId &&
                                       x.Request.RequestType == "LATE_ARRIVAL" &&
                                       x.Request.Status == "APPROVED",
                                  cancellationToken);
                    if (hasConflict)
                    {
                        throw new InvalidOperationException($"An approved late arrival request already exists for assignment #{tard.AssignmentId}.");
                    }

                    // If an approved EARLY_LEAVE exists for this assignment, requested late arrival must be strictly BEFORE early leave
                    var approvedEarly = await _context.TimeAdjustmentRequestDetails
                        .Include(x => x.Request)
                        .FirstOrDefaultAsync(x => x.AssignmentId == tard.AssignmentId &&
                                                  x.RequestId != requestId &&
                                                  x.Request.RequestType == "EARLY_LEAVE" &&
                                                  x.Request.Status == "APPROVED",
                                             cancellationToken);
                    if (approvedEarly != null && tard.RequestedTime >= approvedEarly.RequestedTime)
                    {
                        throw new InvalidOperationException($"Requested late arrival time ({tard.RequestedTime:yyyy-MM-dd HH:mm:ss}) must be strictly before already approved early leave time ({approvedEarly.RequestedTime:yyyy-MM-dd HH:mm:ss}).");
                    }
                }
                else if (request.RequestType == "EARLY_LEAVE")
                {
                    if (tard.RequestedTime <= shift.ScheduledStartAt || tard.RequestedTime >= shift.ScheduledEndAt)
                    {
                        throw new InvalidOperationException($"Requested early leave time ({tard.RequestedTime:yyyy-MM-dd HH:mm:ss}) must be between shift start ({shift.ScheduledStartAt:yyyy-MM-dd HH:mm:ss}) and shift end ({shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss}).");
                    }

                    var hasConflict = await _context.TimeAdjustmentRequestDetails
                        .AnyAsync(x => x.AssignmentId == tard.AssignmentId &&
                                       x.RequestId != requestId &&
                                       x.Request.RequestType == "EARLY_LEAVE" &&
                                       x.Request.Status == "APPROVED",
                                  cancellationToken);
                    if (hasConflict)
                    {
                        throw new InvalidOperationException($"An approved early leave request already exists for assignment #{tard.AssignmentId}.");
                    }

                    // If an approved LATE_ARRIVAL exists for this assignment, requested early leave must be strictly AFTER late arrival
                    var approvedLate = await _context.TimeAdjustmentRequestDetails
                        .Include(x => x.Request)
                        .FirstOrDefaultAsync(x => x.AssignmentId == tard.AssignmentId &&
                                                  x.RequestId != requestId &&
                                                  x.Request.RequestType == "LATE_ARRIVAL" &&
                                                  x.Request.Status == "APPROVED",
                                             cancellationToken);
                    if (approvedLate != null && tard.RequestedTime <= approvedLate.RequestedTime)
                    {
                        throw new InvalidOperationException($"Requested early leave time ({tard.RequestedTime:yyyy-MM-dd HH:mm:ss}) must be strictly after already approved late arrival time ({approvedLate.RequestedTime:yyyy-MM-dd HH:mm:ss}).");
                    }
                }

                // If attendance already exists for this assignment, link request ID and calculate excused minutes without changing actual checkin/checkout times
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.AssignmentId == tard.AssignmentId, cancellationToken);

                if (attendance != null)
                {
                    if (request.RequestType == "LATE_ARRIVAL")
                    {
                        attendance.LateArrivalRequestId = request.RequestId;
                        if (attendance.CheckInTime.HasValue)
                        {
                            var requestedLateMinutes = Math.Max(0, (int)(tard.RequestedTime - shift.ScheduledStartAt).TotalMinutes);
                            var actualLateMinutes = Math.Max(0, (int)(attendance.CheckInTime.Value - shift.ScheduledStartAt).TotalMinutes);
                            attendance.TotalLateMinutes = actualLateMinutes;
                            attendance.ExcusedLateMinutes = Math.Min(actualLateMinutes, requestedLateMinutes);
                            attendance.ChargeableLateMinutes = Math.Max(0, actualLateMinutes - attendance.ExcusedLateMinutes);
                        }
                        attendance.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (request.RequestType == "EARLY_LEAVE")
                    {
                        attendance.EarlyLeaveRequestId = request.RequestId;
                        if (attendance.CheckOutTime.HasValue)
                        {
                            var requestedEarlyMinutes = Math.Max(0, (int)(shift.ScheduledEndAt - tard.RequestedTime).TotalMinutes);
                            var actualEarlyMinutes = Math.Max(0, (int)(shift.ScheduledEndAt - attendance.CheckOutTime.Value).TotalMinutes);
                            attendance.TotalEarlyLeaveMinutes = actualEarlyMinutes;
                            attendance.ExcusedEarlyLeaveMinutes = Math.Min(actualEarlyMinutes, requestedEarlyMinutes);
                            attendance.ChargeableEarlyLeaveMinutes = Math.Max(0, actualEarlyMinutes - attendance.ExcusedEarlyLeaveMinutes);
                        }
                        attendance.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                request.RequestId,
                request.UserId,
                request.BranchId,
                request.RequestType,
                request.Reason,
                request.Status,
                request.AppliedRequestLimitRuleId,
                request.ReviewedBy,
                request.ReviewedAt,
                request.ReviewNote,
                request.CreatedAt,
                request.UpdatedAt
            });

            request.Status = "APPROVED";
            request.ReviewedBy = managerId;
            request.ReviewedAt = now;
            request.ReviewNote = reviewNote?.Trim();
            request.UpdatedAt = now;

            var newValue = JsonSerializer.Serialize(new
            {
                request.RequestId,
                request.UserId,
                request.BranchId,
                request.RequestType,
                request.Reason,
                request.Status,
                request.AppliedRequestLimitRuleId,
                request.ReviewedBy,
                request.ReviewedAt,
                request.ReviewNote,
                request.CreatedAt,
                request.UpdatedAt
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = managerId,
                Action = "APPROVE",
                TableName = "employee_requests",
                RecordId = request.RequestId.ToString(),
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return request;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Request was modified concurrently. Please reload.", ex);
        }
    }

    public async Task<EmployeeRequest> RejectRequestAsync(
        int requestId,
        int branchId,
        Guid managerId,
        string reviewNote,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // Acquire row lock to prevent race conditions
            var lockedId = await _context.EmployeeRequests
                .FromSqlInterpolated($"SELECT * FROM public.employee_requests WHERE request_id = {requestId} AND branch_id = {branchId} FOR UPDATE")
                .Select(r => (int?)r.RequestId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!lockedId.HasValue)
            {
                throw new KeyNotFoundException("Request not found in this branch.");
            }

            var request = await _context.EmployeeRequests
                .Include(r => r.User)
                .Include(r => r.Branch)
                .SingleAsync(r => r.RequestId == requestId, cancellationToken);

            if (request.RequestType != "LEAVE" && request.RequestType != "LATE_ARRIVAL" && request.RequestType != "EARLY_LEAVE" && request.RequestType != "SALARY_INCREASE")
            {
                throw new KeyNotFoundException("Request type is not supported.");
            }

            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"Request is already {request.Status} and cannot be rejected.");
            }

            var now = DateTime.UtcNow;
            var oldValue = JsonSerializer.Serialize(new
            {
                request.RequestId,
                request.UserId,
                request.BranchId,
                request.RequestType,
                request.Reason,
                request.Status,
                request.AppliedRequestLimitRuleId,
                request.ReviewedBy,
                request.ReviewedAt,
                request.ReviewNote,
                request.CreatedAt,
                request.UpdatedAt
            });

            request.Status = "REJECTED";
            request.ReviewedBy = managerId;
            request.ReviewedAt = now;
            request.ReviewNote = reviewNote.Trim();
            request.UpdatedAt = now;

            var newValue = JsonSerializer.Serialize(new
            {
                request.RequestId,
                request.UserId,
                request.BranchId,
                request.RequestType,
                request.Reason,
                request.Status,
                request.AppliedRequestLimitRuleId,
                request.ReviewedBy,
                request.ReviewedAt,
                request.ReviewNote,
                request.CreatedAt,
                request.UpdatedAt
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = managerId,
                Action = "REJECT",
                TableName = "employee_requests",
                RecordId = request.RequestId.ToString(),
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return request;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Request was modified concurrently. Please reload.", ex);
        }
    }
}
