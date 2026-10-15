using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public partial class StaffEmployeeRequestRepo : IStaffEmployeeRequestRepo
{
    private readonly AppDbContext _context;

    public StaffEmployeeRequestRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.UserId == staffId && u.DeletedAt == null && u.Status == "ACTIVE", cancellationToken);
    }

    public async Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        Guid staffId,
        string? reasonSearch,
        string? requestType,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default,
        bool sortByUpdatedAt = false)
    {
        var query = _context.EmployeeRequests.AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Branch)
            .Include(r => r.SalaryIncreaseRequestDetail).ThenInclude(d => d!.CurrentContract)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
            .Where(r => r.UserId == staffId &&
                        (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"));

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(r => r.RequestType == requestType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt < toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(reasonSearch))
        {
            var trimmed = reasonSearch.Trim();
            query = query.Where(r => r.Reason != null && EF.Functions.ILike(r.Reason, $"%{trimmed}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => sortByUpdatedAt ? r.UpdatedAt : r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmployeeRequest?> GetByIdAsync(
        int requestId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeRequests.AsNoTracking()
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
                                      r.UserId == staffId &&
                                      (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"),
                                 cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetEligibleShiftsAsync(
        Guid staffId,
        int branchId,
        string requestType,
        DateOnly todayVn,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetActiveRuleAsync(branchId, requestType, todayVn, cancellationToken);
        if (rule == null)
        {
            return Array.Empty<ShiftAssignment>();
        }

        ValidateRuleSupported(rule);

        var nowUtc = DateTime.UtcNow;

        var assignments = await _context.ShiftAssignments.AsNoTracking()
            .Include(a => a.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(a => a.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(a => a.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Request)
            .Include(a => a.TimeAdjustmentRequestDetails)
                .ThenInclude(tard => tard.Request)
            .Where(a => a.UserId == staffId &&
                        a.Status == "ASSIGNED" &&
                        a.Shift.Status == "SCHEDULED" &&
                        a.Shift.ScheduledStartAt > nowUtc &&
                        a.Shift.ScheduleWeek.BranchId == branchId)
            .OrderBy(a => a.Shift.ScheduledStartAt)
            .ToListAsync(cancellationToken);

        var eligible = new List<ShiftAssignment>();
        foreach (var a in assignments)
        {
            var shiftDateVn = DateOnly.FromDateTime(a.Shift.ScheduledStartAt.AddHours(7));
            if (shiftDateVn.DayNumber - todayVn.DayNumber < rule.MinimumAdvanceDays)
            {
                continue;
            }

            // Exclude if already has PENDING or APPROVED request of same type
            // Exclude cross-conflict between LEAVE and time adjustment
            if (requestType == "LEAVE")
            {
                var hasLeave = a.LeaveRequestAssignments.Any(l => l.Request.Status is "PENDING" or "APPROVED");
                var hasAdjustment = a.TimeAdjustmentRequestDetails.Any(t => t.Request.Status is "PENDING" or "APPROVED");
                if (hasLeave || hasAdjustment) continue;
            }
            else if (requestType == "LATE_ARRIVAL")
            {
                var hasLeave = a.LeaveRequestAssignments.Any(l => l.Request.Status is "PENDING" or "APPROVED");
                var hasLate = a.TimeAdjustmentRequestDetails.Any(t => t.Request.RequestType == "LATE_ARRIVAL" && t.Request.Status is "PENDING" or "APPROVED");
                if (hasLeave || hasLate) continue;
            }
            else if (requestType == "EARLY_LEAVE")
            {
                var hasLeave = a.LeaveRequestAssignments.Any(l => l.Request.Status is "PENDING" or "APPROVED");
                var hasEarly = a.TimeAdjustmentRequestDetails.Any(t => t.Request.RequestType == "EARLY_LEAVE" && t.Request.Status is "PENDING" or "APPROVED");
                if (hasLeave || hasEarly) continue;
            }

            eligible.Add(a);
        }

        return eligible;
    }

    public async Task<EmployeeRequest> SubmitLeaveRequestAsync(
        Guid staffId,
        int branchId,
        List<int> assignmentIds,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // Lock Staff row to serialize submissions and prevent monthly limit race conditions
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT user_id FROM public.users WHERE user_id = {staffId} FOR UPDATE",
                cancellationToken);

            var rule = await GetActiveRuleAsync(branchId, "LEAVE", todayVn, cancellationToken)
                ?? throw new InvalidOperationException("No active request limit rule found for leave requests in your branch.");

            ValidateRuleSupported(rule);

            await ValidateMonthlyLimitAsync(staffId, "LEAVE", rule.MaxRequestsPerMonth, todayVn, cancellationToken);

            var sortedIds = assignmentIds.Distinct().OrderBy(id => id).ToList();
            foreach (var aid in sortedIds)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT assignment_id FROM public.shift_assignments WHERE assignment_id = {aid} FOR UPDATE",
                    cancellationToken);
            }

            var assignments = await _context.ShiftAssignments
                .Include(a => a.Shift)
                    .ThenInclude(s => s.ScheduleWeek)
                .Where(a => sortedIds.Contains(a.AssignmentId))
                .ToListAsync(cancellationToken);

            if (assignments.Count != sortedIds.Count)
            {
                throw new InvalidOperationException("One or more selected shift assignments were not found.");
            }

            var nowUtc = DateTime.UtcNow;

            foreach (var a in assignments)
            {
                if (a.UserId != staffId)
                {
                    throw new InvalidOperationException("Shift assignment does not belong to you.");
                }
                if (a.Status != "ASSIGNED")
                {
                    throw new InvalidOperationException($"Assignment #{a.AssignmentId} is in status '{a.Status}'; only 'ASSIGNED' can be submitted.");
                }
                if (a.Shift.Status != "SCHEDULED")
                {
                    throw new InvalidOperationException($"Shift #{a.Shift.ShiftId} is in status '{a.Shift.Status}'; only 'SCHEDULED' shifts can be submitted.");
                }
                if (a.Shift.ScheduleWeek.BranchId != branchId)
                {
                    throw new InvalidOperationException("Shift assignment does not belong to your active branch.");
                }
                if (nowUtc >= a.Shift.ScheduledStartAt)
                {
                    throw new InvalidOperationException($"Shift #{a.Shift.ShiftId} has already started or ended.");
                }

                var shiftDateVn = DateOnly.FromDateTime(a.Shift.ScheduledStartAt.AddHours(7));
                if (shiftDateVn.DayNumber - todayVn.DayNumber < rule.MinimumAdvanceDays)
                {
                    throw new InvalidOperationException($"Leave request for shift on {shiftDateVn:dd/MM/yyyy} must be submitted at least {rule.MinimumAdvanceDays} day(s) in advance.");
                }

                // Check conflicting PENDING or APPROVED requests
                var hasLeaveConflict = await _context.LeaveRequestAssignments
                    .AnyAsync(l => l.AssignmentId == a.AssignmentId &&
                                   (l.Request.Status == "PENDING" || l.Request.Status == "APPROVED"),
                              cancellationToken);
                if (hasLeaveConflict)
                {
                    throw new InvalidOperationException($"A pending or approved leave request already exists for assignment #{a.AssignmentId}.");
                }

                var hasTimeAdjustmentConflict = await _context.TimeAdjustmentRequestDetails
                    .AnyAsync(t => t.AssignmentId == a.AssignmentId &&
                                   (t.Request.Status == "PENDING" || t.Request.Status == "APPROVED"),
                              cancellationToken);
                if (hasTimeAdjustmentConflict)
                {
                    throw new InvalidOperationException($"A pending or approved time adjustment request already exists for assignment #{a.AssignmentId}.");
                }
            }

            var employeeRequest = new EmployeeRequest
            {
                UserId = staffId,
                BranchId = branchId,
                RequestType = "LEAVE",
                Reason = reason.Trim(),
                Status = "PENDING",
                AppliedRequestLimitRuleId = rule.RequestLimitRuleId,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            foreach (var aid in sortedIds)
            {
                employeeRequest.LeaveRequestAssignments.Add(new LeaveRequestAssignment
                {
                    AssignmentId = aid,
                    CreatedAt = nowUtc
                });
            }

            _context.EmployeeRequests.Add(employeeRequest);
            await _context.SaveChangesAsync(cancellationToken);

            var newValue = JsonSerializer.Serialize(new
            {
                employeeRequest.RequestId,
                employeeRequest.UserId,
                employeeRequest.BranchId,
                employeeRequest.RequestType,
                employeeRequest.Reason,
                employeeRequest.Status,
                employeeRequest.AppliedRequestLimitRuleId,
                employeeRequest.CreatedAt,
                employeeRequest.UpdatedAt,
                AssignmentIds = sortedIds
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = staffId,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = employeeRequest.RequestId.ToString(),
                NewValue = newValue,
                CreatedAt = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return employeeRequest;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Shift assignment was modified concurrently. Please reload.", ex);
        }
    }

    public async Task<EmployeeRequest> SubmitLateArrivalRequestAsync(
        Guid staffId,
        int branchId,
        int assignmentId,
        DateTime requestedTimeUtc,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT user_id FROM public.users WHERE user_id = {staffId} FOR UPDATE",
                cancellationToken);

            var rule = await GetActiveRuleAsync(branchId, "LATE_ARRIVAL", todayVn, cancellationToken)
                ?? throw new InvalidOperationException("No active request limit rule found for late arrival requests in your branch.");

            ValidateRuleSupported(rule);

            await ValidateMonthlyLimitAsync(staffId, "LATE_ARRIVAL", rule.MaxRequestsPerMonth, todayVn, cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT assignment_id FROM public.shift_assignments WHERE assignment_id = {assignmentId} FOR UPDATE",
                cancellationToken);

            var assignment = await _context.ShiftAssignments
                .Include(a => a.Shift)
                    .ThenInclude(s => s.ScheduleWeek)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken)
                ?? throw new InvalidOperationException("Shift assignment not found.");

            if (assignment.UserId != staffId)
            {
                throw new InvalidOperationException("Shift assignment does not belong to you.");
            }
            if (assignment.Status != "ASSIGNED")
            {
                throw new InvalidOperationException($"Assignment #{assignment.AssignmentId} is in status '{assignment.Status}'; only 'ASSIGNED' can be submitted.");
            }
            if (assignment.Shift.Status != "SCHEDULED")
            {
                throw new InvalidOperationException($"Shift #{assignment.Shift.ShiftId} is in status '{assignment.Shift.Status}'; only 'SCHEDULED' shifts can be submitted.");
            }
            if (assignment.Shift.ScheduleWeek.BranchId != branchId)
            {
                throw new InvalidOperationException("Shift assignment does not belong to your active branch.");
            }

            var nowUtc = DateTime.UtcNow;
            if (nowUtc >= assignment.Shift.ScheduledStartAt)
            {
                throw new InvalidOperationException($"Shift #{assignment.Shift.ShiftId} has already started or ended.");
            }

            var shiftDateVn = DateOnly.FromDateTime(assignment.Shift.ScheduledStartAt.AddHours(7));
            if (shiftDateVn.DayNumber - todayVn.DayNumber < rule.MinimumAdvanceDays)
            {
                throw new InvalidOperationException($"Late arrival request for shift on {shiftDateVn:dd/MM/yyyy} must be submitted at least {rule.MinimumAdvanceDays} day(s) in advance.");
            }

            var shift = assignment.Shift;
            if (requestedTimeUtc <= shift.ScheduledStartAt || requestedTimeUtc >= shift.ScheduledEndAt)
            {
                throw new InvalidOperationException($"Requested late arrival time ({requestedTimeUtc:yyyy-MM-dd HH:mm:ss} UTC) must be between shift start ({shift.ScheduledStartAt:yyyy-MM-dd HH:mm:ss} UTC) and shift end ({shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss} UTC).");
            }

            // Check conflicting LEAVE
            var hasLeave = await _context.LeaveRequestAssignments
                .AnyAsync(l => l.AssignmentId == assignmentId && (l.Request.Status == "PENDING" || l.Request.Status == "APPROVED"), cancellationToken);
            if (hasLeave)
            {
                throw new InvalidOperationException($"A pending or approved leave request already exists for assignment #{assignmentId}.");
            }

            // Check conflicting LATE_ARRIVAL
            var hasLate = await _context.TimeAdjustmentRequestDetails
                .AnyAsync(t => t.AssignmentId == assignmentId && t.Request.RequestType == "LATE_ARRIVAL" && (t.Request.Status == "PENDING" || t.Request.Status == "APPROVED"), cancellationToken);
            if (hasLate)
            {
                throw new InvalidOperationException($"A pending or approved late arrival request already exists for assignment #{assignmentId}.");
            }

            // Check if existing EARLY_LEAVE is after requested late arrival
            var earlyLeave = await _context.TimeAdjustmentRequestDetails
                .Include(t => t.Request)
                .FirstOrDefaultAsync(t => t.AssignmentId == assignmentId && t.Request.RequestType == "EARLY_LEAVE" && (t.Request.Status == "PENDING" || t.Request.Status == "APPROVED"), cancellationToken);
            if (earlyLeave != null && requestedTimeUtc >= earlyLeave.RequestedTime)
            {
                throw new InvalidOperationException($"Requested late arrival time ({requestedTimeUtc:yyyy-MM-dd HH:mm:ss} UTC) must be strictly before existing early leave time ({earlyLeave.RequestedTime:yyyy-MM-dd HH:mm:ss} UTC).");
            }

            var employeeRequest = new EmployeeRequest
            {
                UserId = staffId,
                BranchId = branchId,
                RequestType = "LATE_ARRIVAL",
                Reason = reason.Trim(),
                Status = "PENDING",
                AppliedRequestLimitRuleId = rule.RequestLimitRuleId,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                TimeAdjustmentRequestDetail = new TimeAdjustmentRequestDetail
                {
                    AssignmentId = assignmentId,
                    RequestedTime = requestedTimeUtc
                }
            };

            _context.EmployeeRequests.Add(employeeRequest);
            await _context.SaveChangesAsync(cancellationToken);

            var newValue = JsonSerializer.Serialize(new
            {
                employeeRequest.RequestId,
                employeeRequest.UserId,
                employeeRequest.BranchId,
                employeeRequest.RequestType,
                employeeRequest.Reason,
                employeeRequest.Status,
                employeeRequest.AppliedRequestLimitRuleId,
                employeeRequest.CreatedAt,
                employeeRequest.UpdatedAt,
                AssignmentId = assignmentId,
                RequestedTime = requestedTimeUtc
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = staffId,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = employeeRequest.RequestId.ToString(),
                NewValue = newValue,
                CreatedAt = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return employeeRequest;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Shift assignment was modified concurrently. Please reload.", ex);
        }
    }

    public async Task<EmployeeRequest> SubmitEarlyLeaveRequestAsync(
        Guid staffId,
        int branchId,
        int assignmentId,
        DateTime requestedTimeUtc,
        string reason,
        DateOnly todayVn,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT user_id FROM public.users WHERE user_id = {staffId} FOR UPDATE",
                cancellationToken);

            var rule = await GetActiveRuleAsync(branchId, "EARLY_LEAVE", todayVn, cancellationToken)
                ?? throw new InvalidOperationException("No active request limit rule found for early leave requests in your branch.");

            ValidateRuleSupported(rule);

            await ValidateMonthlyLimitAsync(staffId, "EARLY_LEAVE", rule.MaxRequestsPerMonth, todayVn, cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT assignment_id FROM public.shift_assignments WHERE assignment_id = {assignmentId} FOR UPDATE",
                cancellationToken);

            var assignment = await _context.ShiftAssignments
                .Include(a => a.Shift)
                    .ThenInclude(s => s.ScheduleWeek)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken)
                ?? throw new InvalidOperationException("Shift assignment not found.");

            if (assignment.UserId != staffId)
            {
                throw new InvalidOperationException("Shift assignment does not belong to you.");
            }
            if (assignment.Status != "ASSIGNED")
            {
                throw new InvalidOperationException($"Assignment #{assignment.AssignmentId} is in status '{assignment.Status}'; only 'ASSIGNED' can be submitted.");
            }
            if (assignment.Shift.Status != "SCHEDULED")
            {
                throw new InvalidOperationException($"Shift #{assignment.Shift.ShiftId} is in status '{assignment.Shift.Status}'; only 'SCHEDULED' shifts can be submitted.");
            }
            if (assignment.Shift.ScheduleWeek.BranchId != branchId)
            {
                throw new InvalidOperationException("Shift assignment does not belong to your active branch.");
            }

            var nowUtc = DateTime.UtcNow;
            if (nowUtc >= assignment.Shift.ScheduledStartAt)
            {
                throw new InvalidOperationException($"Shift #{assignment.Shift.ShiftId} has already started or ended.");
            }

            var shiftDateVn = DateOnly.FromDateTime(assignment.Shift.ScheduledStartAt.AddHours(7));
            if (shiftDateVn.DayNumber - todayVn.DayNumber < rule.MinimumAdvanceDays)
            {
                throw new InvalidOperationException($"Early leave request for shift on {shiftDateVn:dd/MM/yyyy} must be submitted at least {rule.MinimumAdvanceDays} day(s) in advance.");
            }

            var shift = assignment.Shift;
            if (requestedTimeUtc <= shift.ScheduledStartAt || requestedTimeUtc >= shift.ScheduledEndAt)
            {
                throw new InvalidOperationException($"Requested early leave time ({requestedTimeUtc:yyyy-MM-dd HH:mm:ss} UTC) must be between shift start ({shift.ScheduledStartAt:yyyy-MM-dd HH:mm:ss} UTC) and shift end ({shift.ScheduledEndAt:yyyy-MM-dd HH:mm:ss} UTC).");
            }

            // Check conflicting LEAVE
            var hasLeave = await _context.LeaveRequestAssignments
                .AnyAsync(l => l.AssignmentId == assignmentId && (l.Request.Status == "PENDING" || l.Request.Status == "APPROVED"), cancellationToken);
            if (hasLeave)
            {
                throw new InvalidOperationException($"A pending or approved leave request already exists for assignment #{assignmentId}.");
            }

            // Check conflicting EARLY_LEAVE
            var hasEarly = await _context.TimeAdjustmentRequestDetails
                .AnyAsync(t => t.AssignmentId == assignmentId && t.Request.RequestType == "EARLY_LEAVE" && (t.Request.Status == "PENDING" || t.Request.Status == "APPROVED"), cancellationToken);
            if (hasEarly)
            {
                throw new InvalidOperationException($"A pending or approved early leave request already exists for assignment #{assignmentId}.");
            }

            // Check if existing LATE_ARRIVAL is before requested early leave
            var lateArrival = await _context.TimeAdjustmentRequestDetails
                .Include(t => t.Request)
                .FirstOrDefaultAsync(t => t.AssignmentId == assignmentId && t.Request.RequestType == "LATE_ARRIVAL" && (t.Request.Status == "PENDING" || t.Request.Status == "APPROVED"), cancellationToken);
            if (lateArrival != null && requestedTimeUtc <= lateArrival.RequestedTime)
            {
                throw new InvalidOperationException($"Requested early leave time ({requestedTimeUtc:yyyy-MM-dd HH:mm:ss} UTC) must be strictly after existing late arrival time ({lateArrival.RequestedTime:yyyy-MM-dd HH:mm:ss} UTC).");
            }

            var employeeRequest = new EmployeeRequest
            {
                UserId = staffId,
                BranchId = branchId,
                RequestType = "EARLY_LEAVE",
                Reason = reason.Trim(),
                Status = "PENDING",
                AppliedRequestLimitRuleId = rule.RequestLimitRuleId,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                TimeAdjustmentRequestDetail = new TimeAdjustmentRequestDetail
                {
                    AssignmentId = assignmentId,
                    RequestedTime = requestedTimeUtc
                }
            };

            _context.EmployeeRequests.Add(employeeRequest);
            await _context.SaveChangesAsync(cancellationToken);

            var newValue = JsonSerializer.Serialize(new
            {
                employeeRequest.RequestId,
                employeeRequest.UserId,
                employeeRequest.BranchId,
                employeeRequest.RequestType,
                employeeRequest.Reason,
                employeeRequest.Status,
                employeeRequest.AppliedRequestLimitRuleId,
                employeeRequest.CreatedAt,
                employeeRequest.UpdatedAt,
                AssignmentId = assignmentId,
                RequestedTime = requestedTimeUtc
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = staffId,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = employeeRequest.RequestId.ToString(),
                NewValue = newValue,
                CreatedAt = nowUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return employeeRequest;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Shift assignment was modified concurrently. Please reload.", ex);
        }
    }

    public async Task<EmployeeRequest> CancelRequestAsync(
        int requestId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var lockedId = await _context.EmployeeRequests
                .FromSqlInterpolated($"SELECT * FROM public.employee_requests WHERE request_id = {requestId} AND user_id = {staffId} FOR UPDATE")
                .Select(r => (int?)r.RequestId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!lockedId.HasValue)
            {
                throw new KeyNotFoundException("Request not found.");
            }

            var request = await _context.EmployeeRequests
                .SingleAsync(r => r.RequestId == requestId, cancellationToken);

            if (request.RequestType != "LEAVE" && request.RequestType != "LATE_ARRIVAL" && request.RequestType != "EARLY_LEAVE" && request.RequestType != "SALARY_INCREASE")
            {
                throw new KeyNotFoundException("Request type is not supported.");
            }

            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"Request is already {request.Status} and cannot be cancelled.");
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

            request.Status = "CANCELLED";
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
                UserId = staffId,
                Action = "CANCEL",
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

    private async Task<RequestLimitRule?> GetActiveRuleAsync(
        int branchId,
        string requestType,
        DateOnly todayVn,
        CancellationToken cancellationToken)
    {
        return await _context.RequestLimitRules.AsNoTracking()
            .Where(r => r.BranchId == branchId &&
                        r.RequestType == requestType &&
                        r.Status == "ACTIVE" &&
                        r.EffectiveFrom <= todayVn &&
                        (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= todayVn))
            .OrderByDescending(r => r.EffectiveFrom)
            .ThenByDescending(r => r.RequestLimitRuleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ValidateRuleSupported(RequestLimitRule rule)
    {
        if (rule.SubmissionDeadlineDayOfWeek.HasValue)
        {
            throw new InvalidOperationException($"Quy tắc giới hạn (Rule #{rule.RequestLimitRuleId}) có cấu hình hạn nộp (submission_deadline_day_of_week = {rule.SubmissionDeadlineDayOfWeek.Value}) nhưng chưa có đặc tả nghiệp vụ xác thực chính thức. Cần thống nhất quy tắc hạn nộp với module Request Limit trước khi áp dụng.");
        }
    }

    private async Task ValidateMonthlyLimitAsync(
        Guid staffId,
        string requestType,
        int maxRequestsPerMonth,
        DateOnly todayVn,
        CancellationToken cancellationToken)
    {
        var startOfMonthVn = new DateOnly(todayVn.Year, todayVn.Month, 1);
        var startOfNextMonthVn = startOfMonthVn.AddMonths(1);

        // Convert VN month boundary to UTC [fromUtc, toUtcExclusive)
        var fromUtc = DateTime.SpecifyKind(startOfMonthVn.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(startOfNextMonthVn.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);

        var countInMonth = await _context.EmployeeRequests
            .Where(r => r.UserId == staffId &&
                        r.RequestType == requestType &&
                        r.CreatedAt >= fromUtc &&
                        r.CreatedAt < toUtc)
            .CountAsync(cancellationToken);

        if (countInMonth >= maxRequestsPerMonth)
        {
            throw new InvalidOperationException($"You have reached the limit of {maxRequestsPerMonth} {requestType} request(s) for this month ({todayVn:MM/yyyy}).");
        }
    }
}
