using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public sealed class StaffTimesheetRepo : IStaffTimesheetRepo
{
    private readonly AppDbContext _context;

    public StaffTimesheetRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.UserId == staffId && u.Status == "ACTIVE", cancellationToken);
    }

    public Task<List<WorkScheduleWeek>> GetAvailableWeeksForStaffAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Where(w => w.BranchId == branchId &&
                        w.Status != "CANCELLED" &&
                        (w.PayrollPeriod != null || w.Status == "REGISTRATION_CLOSED" || w.Status == "COMPLETED"))
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkScheduleWeek?> GetWeeklyScheduleWithStaffShiftsAsync(
        int scheduleWeekId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
                    .ThenInclude(t => t.TimesheetAdjustmentRequests)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.Attendance)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);
    }

    public async Task<Timesheet?> GetStaffTimesheetForWeekAsync(
        int scheduleWeekId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var week = await _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.PayrollPeriod)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId, cancellationToken);

        if (week?.PayrollPeriod == null)
            return null;

        return await _context.Timesheets
            .FirstOrDefaultAsync(t => t.PayrollPeriodId == week.PayrollPeriod.PayrollPeriodId && t.UserId == staffId, cancellationToken);
    }

    public async Task<bool> ConfirmTimesheetAsync(
        int timesheetId,
        Guid staffId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.PayrollPeriod)
                .ThenInclude(p => p.ScheduleWeek)
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId && t.UserId == staffId, cancellationToken);

        if (timesheet == null)
            return false;

        if (string.Equals(timesheet.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) || string.Equals(timesheet.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot confirm: Timesheet is already reviewed or locked.");

        if (string.Equals(timesheet.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This timesheet has already been confirmed.");

        var now = DateTime.UtcNow;
        timesheet.StaffReviewStatus = "CONFIRMED";
        timesheet.StaffReviewedAt = now;
        timesheet.StaffReviewNote = note;
        timesheet.UpdatedAt = now;

        var week = timesheet.PayrollPeriod?.ScheduleWeek;
        if (week != null)
        {
            var branchId = week.BranchId;
            var managerIds = await _context.Users
                .Where(u => u.BranchId == branchId && u.Role.RoleName == "MANAGER" && u.Status == "ACTIVE")
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            if (managerIds.Any())
            {
                var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId, cancellationToken);
                var staffName = staffUser?.FullName ?? "Staff member";
                var notif = new WorkShiftManagement.DAL.Entities.Notification
                {
                    Title = "Timesheet Confirmed by Staff",
                    Content = $"{staffName} has confirmed their timesheet for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy}.",
                    NotificationType = "TIMESHEET",
                    CreatedBy = staffId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false,
                    TargetType = "Custom",
                    Channels = new List<string> { "InApp" },
                    TargetRoles = new List<string> { "MANAGER" }
                };

                foreach (var mId in managerIds)
                {
                    notif.UserNotifications.Add(new UserNotification
                    {
                        UserId = mId,
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                _context.Notifications.Add(notif);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SubmitAdjustmentRequestsAsync(
        int scheduleWeekId,
        Guid staffId,
        List<StaffAdjustmentSubmissionItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items == null || !items.Any())
        {
            throw new ArgumentException("Adjustment items list cannot be empty.");
        }

        var week = await _context.WorkScheduleWeeks
            .Include(w => w.PayrollPeriod)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId, cancellationToken);

        if (week?.PayrollPeriod == null)
        {
            throw new KeyNotFoundException("Payroll period or work schedule week not found.");
        }

        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.PayrollPeriodId == week.PayrollPeriod.PayrollPeriodId && t.UserId == staffId, cancellationToken);

        if (timesheet == null)
        {
            throw new KeyNotFoundException("Timesheet has not been generated for this week yet.");
        }

        if (string.Equals(timesheet.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) || string.Equals(timesheet.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This timesheet has been reviewed. No adjustments can be submitted.");
        }

        if (string.Equals(timesheet.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You have already confirmed this timesheet. No adjustments can be submitted.");
        }

        // Deadline check
        var firstGeneratedAt = timesheet.GeneratedAt ?? week.PayrollPeriod.CreatedAt;
        var vnOffset = TimeSpan.FromHours(7);
        var generatedAtVn = firstGeneratedAt + vnOffset;
        var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
        var reviewDeadline = week.PayrollPeriod.ApprovalDeadlineAt ?? (endOfDayVn - vnOffset);

        if (DateTime.UtcNow >= reviewDeadline)
        {
            throw new InvalidOperationException("The deadline for submitting timesheet adjustment requests has passed.");
        }

        var assignments = await _context.ShiftAssignments
            .Include(a => a.Attendance)
            .Include(a => a.Shift)
            .Where(a => a.UserId == staffId && a.Shift.ScheduleWeekId == scheduleWeekId && a.Status != "CANCELLED")
            .ToListAsync(cancellationToken);

        var existingRequests = await _context.TimesheetAdjustmentRequests
            .Where(r => r.TimesheetId == timesheet.TimesheetId && r.UserId == staffId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId, cancellationToken);

        var activeRule = await GetActiveAttendanceRuleAsync(cancellationToken);
        int checkInBeforeMinutes = activeRule?.CheckInBeforeMinutes ?? 30;
        int checkOutAfterMinutes = activeRule?.CheckOutAfterMinutes ?? 60;

        // Batch pre-create any missing Attendance records in memory
        var assignmentsNeedingAttendance = assignments
            .Where(a => items.Any(i => i.AssignmentId == a.AssignmentId) && a.Attendance == null)
            .ToList();

        if (assignmentsNeedingAttendance.Any())
        {
            var branch = await _context.Branches.FindAsync(new object[] { week.BranchId }, cancellationToken);

            foreach (var a in assignmentsNeedingAttendance)
            {
                var newAttendance = new Attendance
                {
                    AssignmentId = a.AssignmentId,
                    AttendanceRuleId = activeRule?.AttendanceRuleId ?? 1,
                    RuleSnapshotJson = "{}",
                    EmployeeFullNameSnapshot = staffUser?.FullName ?? "Staff",
                    ShiftStartAtSnapshot = a.Shift.ScheduledStartAt,
                    ShiftEndAtSnapshot = a.Shift.ScheduledEndAt,
                    BranchNameSnapshot = branch?.BranchName ?? $"Branch {week.BranchId}",
                    Status = "INCOMPLETE",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Attendances.Add(newAttendance);
                a.Attendance = newAttendance;
            }
        }

        foreach (var item in items)
        {
            var assignment = assignments.FirstOrDefault(a => a.AssignmentId == item.AssignmentId);
            if (assignment == null)
            {
                throw new ArgumentException($"Shift assignment #{item.AssignmentId} does not belong to your schedule this week.");
            }

            var shift = assignment.Shift;
            int attendanceId = assignment.Attendance!.AttendanceId;

            // Check existing request status for this attendance (most recent request)
            var existingRequest = existingRequests
                .Where(r => r.AttendanceId == attendanceId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (existingRequest != null)
            {
                var reqStatus = existingRequest.Status.ToUpperInvariant();
                if (reqStatus == "PENDING")
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}' already has a pending adjustment request awaiting manager review.");
                if (reqStatus == "REJECTED")
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}' adjustment request was rejected by manager and cannot be resubmitted.");
            }

            if (!item.AdjustCheckIn && !item.AdjustCheckOut)
            {
                throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Please select Check-in and/or Check-out adjustment.");
            }

            if (item.AdjustCheckIn)
            {
                if (!item.RequestedCheckInTime.HasValue)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Please select requested Check-in time.");
                }

                var earliestCheckInUtc = shift.ScheduledStartAt.AddMinutes(-checkInBeforeMinutes);
                if (item.RequestedCheckInTime.Value < earliestCheckInUtc)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-in ({item.RequestedCheckInTime.Value.AddHours(7):HH:mm}) cannot be earlier than {earliestCheckInUtc.AddHours(7):HH:mm} ({checkInBeforeMinutes}m before shift start).");
                }

                if (item.RequestedCheckInTime.Value > now)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-in time cannot be in the future.");
                }
            }

            if (item.AdjustCheckOut)
            {
                if (!item.RequestedCheckOutTime.HasValue)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Please select requested Check-out time.");
                }

                var latestCheckOutUtc = shift.ScheduledEndAt.AddMinutes(checkOutAfterMinutes);
                if (item.RequestedCheckOutTime.Value > latestCheckOutUtc)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-out ({item.RequestedCheckOutTime.Value.AddHours(7):HH:mm}) cannot be later than {latestCheckOutUtc.AddHours(7):HH:mm} ({checkOutAfterMinutes}m after shift end).");
                }

                if (item.RequestedCheckOutTime.Value > now)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-out time cannot be in the future.");
                }
            }

            // Cross-check effective Check-In and Check-Out
            var effectiveIn = item.AdjustCheckIn ? item.RequestedCheckInTime : assignment.Attendance.CheckInTime;
            var effectiveOut = item.AdjustCheckOut ? item.RequestedCheckOutTime : assignment.Attendance.CheckOutTime;

            if (effectiveIn.HasValue && effectiveOut.HasValue)
            {
                if (effectiveIn.Value >= effectiveOut.Value)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Check-in ({effectiveIn.Value.AddHours(7):HH:mm}) must be earlier than check-out ({effectiveOut.Value.AddHours(7):HH:mm}).");
                }

                if ((effectiveOut.Value - effectiveIn.Value).TotalMinutes < 15)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Working duration must be at least 15 minutes.");
                }
            }
            else if (!effectiveIn.HasValue && effectiveOut.HasValue)
            {
                var earliestCheckInUtc = shift.ScheduledStartAt.AddMinutes(-checkInBeforeMinutes);
                if (effectiveOut.Value <= earliestCheckInUtc)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-out time is invalid for this shift.");
                }
            }
            else if (effectiveIn.HasValue && !effectiveOut.HasValue)
            {
                var latestCheckOutUtc = shift.ScheduledEndAt.AddMinutes(checkOutAfterMinutes);
                if (effectiveIn.Value >= latestCheckOutUtc)
                {
                    throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Requested Check-in time is invalid for this shift.");
                }
            }

            if (string.IsNullOrWhiteSpace(item.Reason))
            {
                throw new ArgumentException($"Shift '{shift.ShiftNameSnapshot}': Please provide an adjustment reason.");
            }

            var targetRequest = (existingRequest != null && string.Equals(existingRequest.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                ? existingRequest
                : null;

            if (targetRequest != null)
            {
                targetRequest.ReviewedBy = null;
                targetRequest.ReviewedAt = null;
                targetRequest.ManagerNote = null;
            }
            else
            {
                targetRequest = new TimesheetAdjustmentRequest
                {
                    TimesheetId = timesheet.TimesheetId,
                    AttendanceId = attendanceId,
                    UserId = staffId,
                    CreatedAt = now
                };
                _context.TimesheetAdjustmentRequests.Add(targetRequest);
            }

            targetRequest.CurrentCheckInSnapshot = assignment.Attendance.CheckInTime;
            targetRequest.CurrentCheckOutSnapshot = assignment.Attendance.CheckOutTime;
            targetRequest.AdjustCheckIn = item.AdjustCheckIn;
            targetRequest.RequestedCheckInTime = item.RequestedCheckInTime;
            targetRequest.AdjustCheckOut = item.AdjustCheckOut;
            targetRequest.RequestedCheckOutTime = item.RequestedCheckOutTime;
            targetRequest.Reason = item.Reason.Trim();
            targetRequest.Status = "PENDING";
            targetRequest.UpdatedAt = now;
        }

        timesheet.StaffReviewStatus = "PENDING";
        timesheet.UpdatedAt = now;

        var branchId = week.BranchId;
        var managerIds = await _context.Users
            .Where(u => u.BranchId == branchId && u.Role.RoleName == "MANAGER" && u.Status == "ACTIVE")
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);

        if (managerIds.Any())
        {
            var staffName = staffUser?.FullName ?? "Staff member";
            var notif = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = "New Timesheet Adjustment Request",
                Content = $"{staffName} submitted {items.Count} timesheet adjustment request(s) for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy}.",
                NotificationType = "TIMESHEET",
                CreatedBy = staffId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                Channels = new List<string> { "InApp" },
                TargetRoles = new List<string> { "MANAGER" }
            };

            foreach (var mId in managerIds)
            {
                notif.UserNotifications.Add(new UserNotification
                {
                    UserId = mId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _context.Notifications.Add(notif);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRules
            .Where(r => r.Status == "PUBLISHED" || r.Status == "ACTIVE")
            .OrderByDescending(r => r.PublishedAt ?? DateTime.MinValue)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _context.AttendanceRules.FirstOrDefaultAsync(cancellationToken);
    }
}
