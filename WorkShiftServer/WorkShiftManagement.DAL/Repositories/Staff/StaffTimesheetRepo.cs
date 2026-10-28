using System.Text.Json;
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
                        .ThenInclude(att => att!.AttendancePenaltyRecords)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.LeaveRequestAssignments)
                        .ThenInclude(l => l.Request)
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

        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.PayrollPeriodId == week.PayrollPeriod.PayrollPeriodId && t.UserId == staffId, cancellationToken);

        if (timesheet != null && string.Equals(timesheet.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            var reviewDeadlineUtc = timesheet.ReviewDeadline ?? (timesheet.GeneratedAt ?? week.PayrollPeriod.CreatedAt).AddHours(24);

            if (DateTime.UtcNow >= reviewDeadlineUtc)
            {
                timesheet.StaffReviewStatus = "NO_RESPONSE";
                timesheet.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return timesheet;
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

        if (string.Equals(timesheet.StaffReviewStatus, "ADJUSTMENT_PENDING", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot confirm: You have pending adjustment requests under review by your manager.");

        if (string.Equals(timesheet.StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot confirm: The review deadline has expired and this timesheet is marked as NO_RESPONSE.");

        var reviewDeadlineUtc = timesheet.ReviewDeadline ?? (timesheet.GeneratedAt ?? timesheet.CreatedAt).AddHours(24);
        if (DateTime.UtcNow >= reviewDeadlineUtc)
        {
            timesheet.StaffReviewStatus = "NO_RESPONSE";
            timesheet.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Cannot confirm: The review deadline has expired and this timesheet is marked as NO_RESPONSE.");
        }

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

        if (string.Equals(timesheet.StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The review deadline has expired (NO_RESPONSE). No adjustments can be submitted.");
        }

        // Deadline check
        var reviewDeadline = timesheet.ReviewDeadline ?? (timesheet.GeneratedAt ?? week.PayrollPeriod.CreatedAt).AddHours(24);

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

            var activePenaltyRule = await _context.AttendancePenaltyRules
                .Include(r => r.AttendancePenaltyLevels)
                .Where(r => r.Status == "ACTIVE")
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            string initialRuleSnapshot = "{}";
            if (activeRule != null)
            {
                var snapshotObj = new
                {
                    AttendanceRule = new
                    {
                        activeRule.AttendanceRuleId,
                        activeRule.RuleName,
                        activeRule.CheckInBeforeMinutes,
                        activeRule.CheckInGraceMinutes,
                        activeRule.CheckOutGraceMinutes,
                        activeRule.CheckOutAfterMinutes,
                        activeRule.MaxAttendanceDistanceMeters
                    },
                    AttendancePenaltyRule = activePenaltyRule != null ? new
                    {
                        activePenaltyRule.PenaltyRuleId,
                        activePenaltyRule.RuleName,
                        PenaltyLevels = activePenaltyRule.AttendancePenaltyLevels.Select(l => new
                        {
                            l.PenaltyLevelId,
                            l.ViolationType,
                            l.FromMinutes,
                            l.ToMinutes,
                            l.PenaltyAmount
                        }).ToList()
                    } : null,
                    SnapshottedAt = now
                };
                initialRuleSnapshot = JsonSerializer.Serialize(snapshotObj);
            }

            foreach (var a in assignmentsNeedingAttendance)
            {
                var newAttendance = new Attendance
                {
                    AssignmentId = a.AssignmentId,
                    AttendanceRuleId = activeRule?.AttendanceRuleId ?? 1,
                    RuleSnapshotJson = initialRuleSnapshot,
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

            await _context.SaveChangesAsync(cancellationToken);
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

            // Retrieve all past and current requests for this attendance
            var pastRequests = existingRequests
                .Where(r => r.AttendanceId == attendanceId)
                .ToList();

            // 1. Any pending request blocks this shift until manager finishes review
            if (pastRequests.Any(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}' already has a pending adjustment request awaiting manager review.");
            }

            // 2. Granular lock: Check-In
            var resolvedCheckInReq = pastRequests.FirstOrDefault(r => r.AdjustCheckIn &&
                (string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(r.Status, "REJECTED", StringComparison.OrdinalIgnoreCase)));

            if (item.AdjustCheckIn && resolvedCheckInReq != null)
            {
                if (string.Equals(resolvedCheckInReq.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}': Check-in adjustment has already been approved and cannot be modified again.");
                else
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}': Check-in adjustment was rejected by manager and cannot be requested again.");
            }

            // 3. Granular lock: Check-Out
            var resolvedCheckOutReq = pastRequests.FirstOrDefault(r => r.AdjustCheckOut &&
                (string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(r.Status, "REJECTED", StringComparison.OrdinalIgnoreCase)));

            if (item.AdjustCheckOut && resolvedCheckOutReq != null)
            {
                if (string.Equals(resolvedCheckOutReq.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}': Check-out adjustment has already been approved and cannot be modified again.");
                else
                    throw new InvalidOperationException($"Shift '{shift.ShiftNameSnapshot}': Check-out adjustment was rejected by manager and cannot be requested again.");
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

            var targetRequest = new TimesheetAdjustmentRequest
            {
                TimesheetId = timesheet.TimesheetId,
                AttendanceId = attendanceId,
                Attendance = assignment.Attendance!,
                UserId = staffId,
                CurrentCheckInSnapshot = assignment.Attendance.CheckInTime,
                CurrentCheckOutSnapshot = assignment.Attendance.CheckOutTime,
                AdjustCheckIn = item.AdjustCheckIn,
                RequestedCheckInTime = item.RequestedCheckInTime,
                AdjustCheckOut = item.AdjustCheckOut,
                RequestedCheckOutTime = item.RequestedCheckOutTime,
                Reason = item.Reason.Trim(),
                Status = "PENDING",
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.TimesheetAdjustmentRequests.Add(targetRequest);
        }

        timesheet.StaffReviewStatus = "ADJUSTMENT_PENDING";
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

    public async Task<(TimesheetAdjustmentRequest Request, int ScheduleWeekId)> CancelAdjustmentRequestAsync(
        int adjustmentRequestId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.TimesheetAdjustmentRequests
            .Include(r => r.User)
            .Include(r => r.Timesheet)
                .ThenInclude(t => t.PayrollPeriod)
                    .ThenInclude(p => p.ScheduleWeek)
            .Include(r => r.Attendance)
                .ThenInclude(a => a.Assignment)
                    .ThenInclude(ass => ass.Shift)
            .FirstOrDefaultAsync(r => r.AdjustmentRequestId == adjustmentRequestId && r.UserId == staffId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"Timesheet adjustment request #{adjustmentRequestId} was not found or does not belong to you.");
        }

        if (!string.Equals(request.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only PENDING adjustment requests can be cancelled. Current status is '{request.Status}'.");
        }

        var timesheet = request.Timesheet;
        var week = timesheet?.PayrollPeriod?.ScheduleWeek;
        var now = DateTime.UtcNow;

        if (timesheet != null)
        {
            var reviewDeadlineUtc = timesheet.ReviewDeadline ?? (timesheet.GeneratedAt ?? timesheet.CreatedAt).AddHours(24);
            if (now >= reviewDeadlineUtc)
            {
                throw new InvalidOperationException("The review deadline has expired. You can no longer cancel this adjustment request.");
            }

            if (string.Equals(timesheet.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(timesheet.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This timesheet has already been reviewed or locked.");
            }
        }

        request.Status = "CANCELLED";
        request.UpdatedAt = now;

        if (timesheet != null)
        {
            bool hasOtherPending = await _context.TimesheetAdjustmentRequests
                .AnyAsync(r => r.TimesheetId == timesheet.TimesheetId && r.AdjustmentRequestId != adjustmentRequestId && r.Status == "PENDING", cancellationToken);

            if (!hasOtherPending)
            {
                timesheet.StaffReviewStatus = "PENDING";
                timesheet.UpdatedAt = now;
            }
        }

        var staffName = request.User?.FullName ?? "Staff member";
        var shiftName = request.Attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
        var shiftDate = request.Attendance?.ShiftStartAtSnapshot ?? DateTime.UtcNow;
        var scheduleWeekId = week?.ScheduleWeekId ?? 0;
        var branchId = week?.BranchId ?? request.User?.BranchId ?? 0;

        var managerIds = await _context.Users
            .Where(u => u.BranchId == branchId && u.Role.RoleName == "MANAGER" && u.Status == "ACTIVE")
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);

        string notificationContent = $"Staff {staffName} has cancelled their adjustment request for shift '{shiftName}' on {shiftDate:dd/MM/yyyy}.";

        if (managerIds.Any())
        {
            var notif = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = "Adjustment Request Cancelled",
                Content = notificationContent,
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

        return (request, scheduleWeekId);
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
