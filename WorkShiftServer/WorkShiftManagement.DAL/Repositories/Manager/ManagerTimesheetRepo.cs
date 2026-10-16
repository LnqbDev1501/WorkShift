using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ManagerTimesheetRepo : IManagerTimesheetRepo
{
    private readonly AppDbContext _context;

    public ManagerTimesheetRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.UserId == managerId &&
                u.Status == "ACTIVE" &&
                (u.Role.RoleName == "MANAGER" || u.Role.RoleName == "ADMIN"),
                cancellationToken);
    }

    public async Task<(List<WorkScheduleWeek> Items, int TotalCount)> GetWeeklyTimesheetsAsync(
        int branchId,
        string? search,
        int? scheduleWeekId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkScheduleWeeks
            .AsNoTracking()
            .Where(w => w.BranchId == branchId &&
                        w.Status != "CANCELLED" &&
                        (w.PayrollPeriod != null || w.Status == "REGISTRATION_CLOSED" || w.Status == "COMPLETED"));

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            query = query.Where(w => w.ScheduleWeekId == scheduleWeekId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(w =>
                EF.Functions.ILike(w.Branch.BranchName, searchPattern) ||
                (w.PayrollPeriod != null && EF.Functions.ILike(w.PayrollPeriod.PeriodName, searchPattern)) ||
                (w.PayrollPeriod != null && EF.Functions.ILike(w.PayrollPeriod.PeriodCode, searchPattern)));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.WeekEndDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.WeekStartDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var upperStatus = status.Trim().ToUpperInvariant();
            if (upperStatus == "NOT GENERATED")
            {
                query = query.Where(w => w.PayrollPeriod == null || !w.PayrollPeriod.Timesheets.Any());
            }
            else
            {
                query = query.Where(w =>
                    (w.PayrollPeriod != null && (w.PayrollPeriod.Status == upperStatus || w.PayrollPeriod.Timesheets.Any(t => t.Status == upperStatus))) ||
                    w.Status == upperStatus);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<WorkScheduleWeek>> GetAvailableWeeksForBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Where(w => w.BranchId == branchId &&
                        w.Status != "CANCELLED" &&
                        (w.PayrollPeriod != null || w.Status == "REGISTRATION_CLOSED" || w.Status == "COMPLETED"))
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkScheduleWeek?> GetWeeklyTimesheetDetailAsync(int scheduleWeekId, int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
                    .ThenInclude(t => t.User)
                        .ThenInclude(u => u.Role)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
                    .ThenInclude(t => t.TimesheetAdjustmentRequests)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.User)
                        .ThenInclude(u => u.Role)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.Attendance)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.LeaveRequestAssignments)
                        .ThenInclude(l => l.Request)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);
    }

    public async Task<(WorkScheduleWeek Week, List<Timesheet> Timesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)> GenerateWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var week = await _context.WorkScheduleWeeks
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.Attendance)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);

        if (week == null)
        {
            throw new KeyNotFoundException($"Schedule week with ID {scheduleWeekId} was not found for your branch.");
        }

        if (!string.Equals(week.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot generate timesheet: The schedule week has not finished yet (current status: {week.Status}). Only COMPLETED schedule weeks can be generated.");
        }

        if (week.PayrollPeriod == null)
        {
            throw new InvalidOperationException("Cannot generate timesheet: No payroll period is associated with this schedule week. Please ensure the payroll period is created first.");
        }

        if (!string.Equals(week.PayrollPeriod.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot generate timesheet: The payroll period is not OPEN (current status: {week.PayrollPeriod.Status}). Only periods with OPEN status can have timesheets generated.");
        }

        if (week.PayrollPeriod.Timesheets.Any())
        {
            throw new InvalidOperationException("Timesheet for this week has already been generated and cannot be re-generated.");
        }

        var activeAssignments = week.WorkShifts
            .Where(s => !string.Equals(s.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .SelectMany(s => s.ShiftAssignments.Select(a => new { Shift = s, Assignment = a }))
            .Where(x => !string.Equals(x.Assignment.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var staffGroups = activeAssignments.GroupBy(x => x.Assignment.UserId).ToList();
        var generatedTimesheets = new List<Timesheet>();
        var now = DateTime.UtcNow;

        foreach (var group in staffGroups)
        {
            var userId = group.Key;
            int totalAssigned = group.Count();
            int completedCount = 0;
            int absentCount = 0;
            int incompleteCount = 0;
            int totalScheduledMinutes = 0;
            int totalWorkMinutes = 0;
            int totalLateMinutes = 0;
            int excusedLateMinutes = 0;
            int chargeableLateMinutes = 0;
            int totalEarlyLeaveMinutes = 0;
            int excusedEarlyLeaveMinutes = 0;
            int chargeableEarlyLeaveMinutes = 0;

            foreach (var item in group)
            {
                var shift = item.Shift;
                int scheduledDuration = (int)(shift.ScheduledEndAt - shift.ScheduledStartAt).TotalMinutes;
                totalScheduledMinutes += Math.Max(0, scheduledDuration);

                var attendance = item.Assignment.Attendance;
                if (attendance == null)
                {
                    absentCount++;
                    continue;
                }

                var status = attendance.Status?.ToUpperInvariant();
                if (status is "COMPLETED" or "PRESENT" or "LATE" or "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE")
                {
                    completedCount++;
                }
                else if (status == "INCOMPLETE")
                {
                    incompleteCount++;
                }
                else
                {
                    absentCount++;
                    continue;
                }

                totalWorkMinutes += attendance.ActualWorkingMinutes;
                totalLateMinutes += attendance.TotalLateMinutes;
                excusedLateMinutes += attendance.ExcusedLateMinutes;
                chargeableLateMinutes += attendance.ChargeableLateMinutes;
                totalEarlyLeaveMinutes += attendance.TotalEarlyLeaveMinutes;
                excusedEarlyLeaveMinutes += attendance.ExcusedEarlyLeaveMinutes;
                chargeableEarlyLeaveMinutes += attendance.ChargeableEarlyLeaveMinutes;
            }

            var timesheet = new Timesheet
            {
                UserId = userId,
                PayrollPeriodId = week.PayrollPeriod.PayrollPeriodId,
                TotalAssignedShifts = totalAssigned,
                CompletedShiftCount = completedCount,
                AbsentShiftCount = absentCount,
                IncompleteShiftCount = incompleteCount,
                TotalScheduledMinutes = totalScheduledMinutes,
                TotalWorkMinutes = totalWorkMinutes,
                TotalLateMinutes = totalLateMinutes,
                ExcusedLateMinutes = excusedLateMinutes,
                ChargeableLateMinutes = chargeableLateMinutes,
                TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
                ExcusedEarlyLeaveMinutes = excusedEarlyLeaveMinutes,
                ChargeableEarlyLeaveMinutes = chargeableEarlyLeaveMinutes,
                Status = "GENERATED",
                StaffReviewStatus = "PENDING",
                GeneratedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Timesheets.Add(timesheet);
            generatedTimesheets.Add(timesheet);
        }

        var staffUserIds = staffGroups.Select(g => g.Key).ToList();
        int notificationId = 0;
        string notificationContent = $"Timesheet for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has been generated. Please review your logged hours and submit confirmations or revision requests before the deadline.";

        if (staffUserIds.Count > 0)
        {
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = "Weekly Timesheet Generated",
                Content = notificationContent,
                NotificationType = "TIMESHEET",
                CreatedBy = managerId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                TargetBranchId = null,
                TargetRegionId = null,
                Channels = new List<string> { "InApp" },
                TargetRoles = new List<string> { "STAFF" }
            };

            foreach (var staffId in staffUserIds)
            {
                notification.UserNotifications.Add(new UserNotification
                {
                    UserId = staffId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
            notificationId = notification.NotificationId;
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (week, generatedTimesheets, staffUserIds, notificationId, notificationContent);
    }

    public async Task<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, List<Guid> AccountantUserIds, int NotificationId, int AccountantNotificationId, string NotificationContent, string AccountantNotificationContent)> LockWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var week = await _context.WorkScheduleWeeks
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
                    .ThenInclude(t => t.TimesheetAdjustmentRequests)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);

        if (week == null)
        {
            throw new KeyNotFoundException($"Schedule week with ID {scheduleWeekId} was not found for your branch.");
        }

        if (week.PayrollPeriod == null || !week.PayrollPeriod.Timesheets.Any())
        {
            throw new InvalidOperationException("Cannot review timesheet: Timesheet for this week has not been generated yet.");
        }

        var timesheets = week.PayrollPeriod.Timesheets.ToList();

        if (timesheets.All(t => string.Equals(t.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Status, "LOCKED", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Timesheet for this week has already been reviewed.");
        }

        int pendingAdjustmentsCount = timesheets
            .SelectMany(t => t.TimesheetAdjustmentRequests ?? Enumerable.Empty<TimesheetAdjustmentRequest>())
            .Count(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

        if (pendingAdjustmentsCount > 0)
        {
            throw new InvalidOperationException($"Cannot review timesheet: There are {pendingAdjustmentsCount} pending adjustment request(s) awaiting review. Please approve or reject all requests before reviewing.");
        }

        var firstGeneratedAt = timesheets.Where(t => t.GeneratedAt.HasValue).Min(t => t.GeneratedAt)
            ?? week.PayrollPeriod.CreatedAt;

        // Vietnam Time (UTC+7)
        var vnOffset = TimeSpan.FromHours(7);
        var generatedAtVn = firstGeneratedAt + vnOffset;
        var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
        var reviewDeadlineUtc = week.PayrollPeriod.ApprovalDeadlineAt ?? (endOfDayVn - vnOffset);

        var now = DateTime.UtcNow;
        bool isPastDeadline = now >= reviewDeadlineUtc;
        int totalStaff = timesheets.Count;
        int confirmedStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase));
        bool isAllConfirmed = (totalStaff > 0 && confirmedStaff == totalStaff);

        if (!isAllConfirmed && !isPastDeadline)
        {
            var deadlineVnDisplay = (reviewDeadlineUtc + vnOffset).ToString("HH:mm dd/MM/yyyy");
            throw new InvalidOperationException($"Cannot review timesheet: Requires 100% staff confirmation ({confirmedStaff}/{totalStaff} confirmed) or wait until the review deadline expires at end of day ({deadlineVnDisplay}).");
        }

        foreach (var timesheet in timesheets)
        {
            timesheet.Status = "REVIEWED";
            timesheet.ReviewedAt = now;
            timesheet.ReviewedBy = managerId;
            timesheet.UpdatedAt = now;
        }

        var staffUserIds = timesheets.Select(t => t.UserId).Distinct().ToList();
        int notificationId = 0;
        string notificationContent = $"Timesheet for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has been reviewed by the branch manager.";

        if (staffUserIds.Count > 0)
        {
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = "Weekly Timesheet Reviewed",
                Content = notificationContent,
                NotificationType = "TIMESHEET",
                CreatedBy = managerId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                TargetBranchId = null,
                TargetRegionId = null,
                Channels = new List<string> { "InApp" },
                TargetRoles = new List<string> { "STAFF" }
            };

            foreach (var staffId in staffUserIds)
            {
                notification.UserNotifications.Add(new UserNotification
                {
                    UserId = staffId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
            notificationId = notification.NotificationId;
        }

        // Query active branch accountants for English notification
        var accountantUserIds = await _context.Users
            .Where(u => u.BranchId == branchId && u.Role.RoleName == "ACCOUNTANT" && u.Status == "ACTIVE" && u.DeletedAt == null)
            .Select(u => u.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int accountantNotificationId = 0;
        string accountantNotificationContent = $"Timesheet for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has been reviewed by the branch manager and is ready for payroll processing.";

        if (accountantUserIds.Count > 0)
        {
            var accountantNotification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = "Weekly Timesheet Reviewed",
                Content = accountantNotificationContent,
                NotificationType = "TIMESHEET",
                CreatedBy = managerId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                TargetBranchId = branchId,
                TargetRegionId = null,
                Channels = new List<string> { "InApp" },
                TargetRoles = new List<string> { "ACCOUNTANT" }
            };

            foreach (var accId in accountantUserIds)
            {
                accountantNotification.UserNotifications.Add(new UserNotification
                {
                    UserId = accId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _context.Notifications.Add(accountantNotification);
            await _context.SaveChangesAsync(cancellationToken);
            accountantNotificationId = accountantNotification.NotificationId;
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (week, timesheets, staffUserIds, accountantUserIds, notificationId, accountantNotificationId, notificationContent, accountantNotificationContent);
    }

    public async Task<(List<TimesheetAdjustmentRequest> Items, int TotalCount, int PendingCount, int ApprovedCount, int RejectedCount)> GetTimesheetAdjustmentsAsync(
        int branchId,
        string? search,
        string? status,
        int? scheduleWeekId,
        Guid? staffId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.TimesheetAdjustmentRequests
            .AsNoTracking()
            .Where(r => r.Timesheet.PayrollPeriod.BranchId == branchId);

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            baseQuery = baseQuery.Where(r => r.Timesheet.PayrollPeriod.ScheduleWeekId == scheduleWeekId.Value);
        }

        if (staffId.HasValue && staffId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(r => r.UserId == staffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.User.FullName.ToLower().Contains(searchLower) ||
                (r.User.Email != null && r.User.Email.ToLower().Contains(searchLower)) ||
                (r.User.Phone != null && r.User.Phone.Contains(searchLower)) ||
                r.Attendance.Assignment.Shift.ShiftNameSnapshot.ToLower().Contains(searchLower));
        }

        // Calculate counts before status filtering
        int totalCount = await baseQuery.CountAsync(cancellationToken);
        int pendingCount = await baseQuery.CountAsync(r => r.Status == "PENDING", cancellationToken);
        int approvedCount = await baseQuery.CountAsync(r => r.Status == "APPROVED", cancellationToken);
        int rejectedCount = await baseQuery.CountAsync(r => r.Status == "REJECTED", cancellationToken);

        int filteredCount = totalCount;
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(r => r.Status == normalizedStatus);
            filteredCount = normalizedStatus switch
            {
                "PENDING" => pendingCount,
                "APPROVED" => approvedCount,
                "REJECTED" => rejectedCount,
                _ => await baseQuery.CountAsync(cancellationToken)
            };
        }

        var items = await baseQuery
            .Include(r => r.User)
                .ThenInclude(u => u.Role)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.Timesheet)
                .ThenInclude(t => t.PayrollPeriod)
                    .ThenInclude(p => p.ScheduleWeek)
            .Include(r => r.Attendance)
                .ThenInclude(a => a.Assignment)
                    .ThenInclude(ass => ass.Shift)
            .OrderByDescending(r => r.Status == "PENDING")
            .ThenByDescending(r => r.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, filteredCount, pendingCount, approvedCount, rejectedCount);
    }

    public async Task<TimesheetAdjustmentRequest?> GetAdjustmentDetailAsync(
        int adjustmentRequestId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TimesheetAdjustmentRequests
            .AsNoTracking()
            .Include(r => r.User)
                .ThenInclude(u => u.Role)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.Timesheet)
                .ThenInclude(t => t.PayrollPeriod)
                    .ThenInclude(p => p.ScheduleWeek)
            .Include(r => r.Attendance)
                .ThenInclude(a => a.Assignment)
                    .ThenInclude(ass => ass.Shift)
            .Include(r => r.Attendance)
                .ThenInclude(a => a.AttendanceRule)
            .FirstOrDefaultAsync(r => r.AdjustmentRequestId == adjustmentRequestId && r.Timesheet.PayrollPeriod.BranchId == branchId, cancellationToken);
    }

    public async Task<TimesheetAdjustmentRequest> ApproveAdjustmentRequestAsync(
        int adjustmentRequestId,
        int branchId,
        Guid managerId,
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
            .Include(r => r.Attendance)
                .ThenInclude(a => a.AttendanceRule)
            .FirstOrDefaultAsync(r => r.AdjustmentRequestId == adjustmentRequestId && r.Timesheet.PayrollPeriod.BranchId == branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Timesheet adjustment request #{adjustmentRequestId} not found for your branch.");

        if (!string.Equals(request.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Adjustment request #{adjustmentRequestId} has already been resolved with status '{request.Status}'.");
        }

        var now = DateTime.UtcNow;
        request.Status = "APPROVED";
        request.ReviewedBy = managerId;
        request.ReviewedAt = now;
        request.UpdatedAt = now;

        var attendance = request.Attendance;
        if (attendance != null)
        {
            if (request.AdjustCheckIn && request.RequestedCheckInTime.HasValue)
            {
                attendance.CheckInTime = request.RequestedCheckInTime.Value;
            }

            if (request.AdjustCheckOut && request.RequestedCheckOutTime.HasValue)
            {
                attendance.CheckOutTime = request.RequestedCheckOutTime.Value;
            }

            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                attendance.ActualWorkingMinutes = Math.Max(0, (int)Math.Floor((attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalMinutes));

                var rule = attendance.AttendanceRule;
                int graceIn = rule?.CheckInGraceMinutes ?? 5;
                int graceOut = rule?.CheckOutGraceMinutes ?? 5;

                if (attendance.CheckInTime.Value > attendance.ShiftStartAtSnapshot.AddMinutes(graceIn))
                {
                    attendance.TotalLateMinutes = (int)Math.Max(0, Math.Floor((attendance.CheckInTime.Value - attendance.ShiftStartAtSnapshot).TotalMinutes));
                    attendance.ExcusedLateMinutes = Math.Min(attendance.ExcusedLateMinutes, attendance.TotalLateMinutes);
                    attendance.ChargeableLateMinutes = attendance.TotalLateMinutes - attendance.ExcusedLateMinutes;
                }
                else
                {
                    attendance.TotalLateMinutes = 0;
                    attendance.ExcusedLateMinutes = 0;
                    attendance.ChargeableLateMinutes = 0;
                }

                if (attendance.CheckOutTime.Value < attendance.ShiftEndAtSnapshot.AddMinutes(-graceOut))
                {
                    attendance.TotalEarlyLeaveMinutes = (int)Math.Max(0, Math.Floor((attendance.ShiftEndAtSnapshot - attendance.CheckOutTime.Value).TotalMinutes));
                    attendance.ExcusedEarlyLeaveMinutes = Math.Min(attendance.ExcusedEarlyLeaveMinutes, attendance.TotalEarlyLeaveMinutes);
                    attendance.ChargeableEarlyLeaveMinutes = attendance.TotalEarlyLeaveMinutes - attendance.ExcusedEarlyLeaveMinutes;
                }
                else
                {
                    attendance.TotalEarlyLeaveMinutes = 0;
                    attendance.ExcusedEarlyLeaveMinutes = 0;
                    attendance.ChargeableEarlyLeaveMinutes = 0;
                }

                bool hasLate = attendance.TotalLateMinutes > 0;
                bool hasEarly = attendance.TotalEarlyLeaveMinutes > 0;

                if (hasLate && hasEarly)
                    attendance.Status = "LATE_AND_EARLY_LEAVE";
                else if (hasLate)
                    attendance.Status = "LATE";
                else if (hasEarly)
                    attendance.Status = "EARLY_LEAVE";
                else
                    attendance.Status = "PRESENT";

                if (attendance.Assignment != null)
                {
                    attendance.Assignment.Status = "COMPLETED";
                    attendance.Assignment.UpdatedAt = now;
                }

                attendance.UpdatedAt = now;
            }
        }

        // Recalculate Timesheet aggregates
        var timesheet = request.Timesheet;
        if (timesheet != null)
        {
            var scheduleWeekId = timesheet.PayrollPeriod?.ScheduleWeekId 
                ?? attendance?.Assignment?.Shift?.ScheduleWeekId;

            if (scheduleWeekId.HasValue)
            {
                var assignments = await _context.ShiftAssignments
                    .Include(sa => sa.Attendance)
                    .Where(sa => sa.Shift.ScheduleWeekId == scheduleWeekId.Value
                              && sa.UserId == timesheet.UserId
                              && sa.Shift.Status != "CANCELLED"
                              && sa.Status != "CANCELLED")
                    .ToListAsync(cancellationToken);

                int completedCount = 0;
                int absentCount = 0;
                int incompleteCount = 0;
                int totalWorkMinutes = 0;
                int totalLateMinutes = 0;
                int excusedLateMinutes = 0;
                int chargeableLateMinutes = 0;
                int totalEarlyLeaveMinutes = 0;
                int excusedEarlyLeaveMinutes = 0;
                int chargeableEarlyLeaveMinutes = 0;

                foreach (var assign in assignments)
                {
                    var att = (attendance != null && assign.AssignmentId == attendance.AssignmentId)
                        ? attendance
                        : assign.Attendance;

                    if (att == null)
                    {
                        absentCount++;
                        continue;
                    }

                    var status = att.Status?.ToUpperInvariant();
                    if (status is "COMPLETED" or "PRESENT" or "LATE" or "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE")
                    {
                        completedCount++;
                    }
                    else if (status == "INCOMPLETE")
                    {
                        incompleteCount++;
                    }
                    else
                    {
                        absentCount++;
                        continue;
                    }

                    totalWorkMinutes += att.ActualWorkingMinutes;
                    totalLateMinutes += att.TotalLateMinutes;
                    excusedLateMinutes += att.ExcusedLateMinutes;
                    chargeableLateMinutes += att.ChargeableLateMinutes;
                    totalEarlyLeaveMinutes += att.TotalEarlyLeaveMinutes;
                    excusedEarlyLeaveMinutes += att.ExcusedEarlyLeaveMinutes;
                    chargeableEarlyLeaveMinutes += att.ChargeableEarlyLeaveMinutes;
                }

                timesheet.TotalAssignedShifts = assignments.Count;
                timesheet.CompletedShiftCount = completedCount;
                timesheet.AbsentShiftCount = absentCount;
                timesheet.IncompleteShiftCount = incompleteCount;
                timesheet.TotalWorkMinutes = totalWorkMinutes;
                timesheet.TotalLateMinutes = totalLateMinutes;
                timesheet.ExcusedLateMinutes = excusedLateMinutes;
                timesheet.ChargeableLateMinutes = chargeableLateMinutes;
                timesheet.TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes;
                timesheet.ExcusedEarlyLeaveMinutes = excusedEarlyLeaveMinutes;
                timesheet.ChargeableEarlyLeaveMinutes = chargeableEarlyLeaveMinutes;
                timesheet.UpdatedAt = now;
            }
        }

        // Create In-App Notification in DB for Staff
        var shiftName = attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
        var shiftDate = attendance?.ShiftStartAtSnapshot.ToString("dd/MM/yyyy") ?? "";
        var notif = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = "Timesheet Adjustment Approved",
            Content = $"Your adjustment request for shift '{shiftName}' on {shiftDate} has been approved by the manager. Actual attendance hours have been updated.",
            NotificationType = "TIMESHEET",
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = "Custom",
            Channels = new List<string> { "InApp" },
            TargetRoles = new List<string> { "STAFF" }
        };

        notif.UserNotifications.Add(new UserNotification
        {
            UserId = request.UserId,
            IsRead = false,
            CreatedAt = now
        });

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task<TimesheetAdjustmentRequest> RejectAdjustmentRequestAsync(
        int adjustmentRequestId,
        int branchId,
        Guid managerId,
        string managerNote,
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
            .FirstOrDefaultAsync(r => r.AdjustmentRequestId == adjustmentRequestId && r.Timesheet.PayrollPeriod.BranchId == branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Timesheet adjustment request #{adjustmentRequestId} not found for your branch.");

        if (!string.Equals(request.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Adjustment request #{adjustmentRequestId} has already been resolved with status '{request.Status}'.");
        }

        var now = DateTime.UtcNow;
        request.Status = "REJECTED";
        request.ReviewedBy = managerId;
        request.ReviewedAt = now;
        request.ManagerNote = managerNote.Trim();
        request.UpdatedAt = now;

        // Create In-App Notification in DB for Staff
        var shiftName = request.Attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
        var shiftDate = request.Attendance?.ShiftStartAtSnapshot.ToString("dd/MM/yyyy") ?? "";
        var notif = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = "Timesheet Adjustment Rejected",
            Content = $"Your adjustment request for shift '{shiftName}' on {shiftDate} has been rejected by the manager. Reason: \"{managerNote.Trim()}\".",
            NotificationType = "TIMESHEET",
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = "Custom",
            Channels = new List<string> { "InApp" },
            TargetRoles = new List<string> { "STAFF" }
        };

        notif.UserNotifications.Add(new UserNotification
        {
            UserId = request.UserId,
            IsRead = false,
            CreatedAt = now
        });

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task<List<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)>> AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var vnOffset = TimeSpan.FromHours(7);

        // Find candidate schedule weeks with GENERATED timesheets
        var candidateWeeks = await _context.WorkScheduleWeeks
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
                    .ThenInclude(t => t.TimesheetAdjustmentRequests)
            .Where(w => w.Status != "CANCELLED"
                     && w.PayrollPeriod != null
                     && w.PayrollPeriod.Timesheets.Any(t => t.Status == "GENERATED"))
            .ToListAsync(cancellationToken);

        var results = new List<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)>();

        foreach (var week in candidateWeeks)
        {
            var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
            if (!timesheets.Any()) continue;

            int pendingAdjustmentsCount = timesheets
                .SelectMany(t => t.TimesheetAdjustmentRequests ?? Enumerable.Empty<TimesheetAdjustmentRequest>())
                .Count(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

            // If there are still pending adjustment requests awaiting manager review, do not lock yet
            if (pendingAdjustmentsCount > 0)
                continue;

            var firstGeneratedAt = timesheets.Where(t => t.GeneratedAt.HasValue).Min(t => t.GeneratedAt)
                ?? week.PayrollPeriod!.CreatedAt;
            var generatedAtVn = firstGeneratedAt + vnOffset;
            var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
            var reviewDeadlineUtc = week.PayrollPeriod!.ApprovalDeadlineAt ?? (endOfDayVn - vnOffset);

            if (now >= reviewDeadlineUtc)
            {
                var targetTimesheets = timesheets.Where(t => t.Status == "GENERATED").ToList();
                if (!targetTimesheets.Any()) continue;

                var creatorId = week.PayrollPeriod!.CreatedBy;
                foreach (var t in targetTimesheets)
                {
                    t.Status = "REVIEWED";
                    t.ReviewedAt = now;
                    t.ReviewedBy = creatorId;
                    t.UpdatedAt = now;
                }

                var staffUserIds = targetTimesheets.Select(t => t.UserId).Distinct().ToList();
                int notificationId = 0;
                string notificationContent = $"Timesheet review deadline for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has passed. Timesheets have been finalized and reviewed.";

                if (staffUserIds.Count > 0)
                {
                    var notification = new WorkShiftManagement.DAL.Entities.Notification
                    {
                        Title = "Weekly Timesheet Reviewed",
                        Content = notificationContent,
                        NotificationType = "TIMESHEET",
                        CreatedBy = creatorId,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsDeleted = false,
                        TargetType = "Custom",
                        Channels = new List<string> { "InApp" },
                        TargetRoles = new List<string> { "STAFF" }
                    };

                    foreach (var sId in staffUserIds)
                    {
                        notification.UserNotifications.Add(new UserNotification
                        {
                            UserId = sId,
                            IsRead = false,
                            CreatedAt = now
                        });
                    }

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync(cancellationToken);
                    notificationId = notification.NotificationId;
                }
                else
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                results.Add((week, targetTimesheets, staffUserIds, notificationId, notificationContent));
            }
        }

        return results;
    }
}

