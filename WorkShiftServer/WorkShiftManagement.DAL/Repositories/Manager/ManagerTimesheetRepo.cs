using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Models;

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

    public async Task<WorkScheduleWeek?> GetWeeklyTimesheetDetailAsync(int scheduleWeekId, int branchId, CancellationToken cancellationToken = default)
    {
        var week = await _context.WorkScheduleWeeks
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
                        .ThenInclude(att => att!.AttendancePenaltyRecords)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.LeaveRequestAssignments)
                        .ThenInclude(l => l.Request)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);

        if (week?.PayrollPeriod?.Timesheets != null && week.PayrollPeriod.Timesheets.Count > 0)
        {
            var vnOffset = TimeSpan.FromHours(7);
            var now = DateTime.UtcNow;

            var pendingTimesheets = week.PayrollPeriod.Timesheets
                .Where(t => string.Equals(t.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var expiredTimesheetIds = new List<int>();
            foreach (var t in pendingTimesheets)
            {
                var tGenAt = t.GeneratedAt ?? week.PayrollPeriod.CreatedAt;
                var tGenAtVn = tGenAt + vnOffset;
                var tDeadlineUtc = tGenAtVn.Date.AddDays(1).AddTicks(-1) - vnOffset;
                if (now >= tDeadlineUtc)
                {
                    expiredTimesheetIds.Add(t.TimesheetId);
                }
            }

            if (expiredTimesheetIds.Count > 0)
            {
                await _context.Timesheets
                    .Where(t => expiredTimesheetIds.Contains(t.TimesheetId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.StaffReviewStatus, "NO_RESPONSE")
                        .SetProperty(x => x.UpdatedAt, now), cancellationToken);

                foreach (var t in week.PayrollPeriod.Timesheets.Where(t => expiredTimesheetIds.Contains(t.TimesheetId)))
                {
                    t.StaffReviewStatus = "NO_RESPONSE";
                    t.UpdatedAt = now;
                }
            }
        }

        return week;
    }

    public async Task<(WorkScheduleWeek Week, List<Timesheet> Timesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)> GenerateWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        DateTime? reviewDeadline = null,
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
        if (staffGroups.Count == 0)
        {
            throw new InvalidOperationException("Cannot generate timesheet: No active shifts or assigned staff members were found for this schedule week. Please verify the work schedule before generating timesheets.");
        }

        var generatedTimesheets = new List<Timesheet>();
        var now = DateTime.UtcNow;
        var vnOffset = TimeSpan.FromHours(7);

        DateTime reviewDeadlineUtc;
        if (reviewDeadline.HasValue)
        {
            var reqUtc = reviewDeadline.Value.Kind == DateTimeKind.Utc 
                ? reviewDeadline.Value 
                : DateTime.SpecifyKind(reviewDeadline.Value - vnOffset, DateTimeKind.Utc);

            if (reqUtc < now.AddHours(23).AddMinutes(50))
            {
                throw new ArgumentException("Review deadline must be at least 24 hours from the generation time.");
            }
            if (reqUtc > now.AddHours(72).AddMinutes(10))
            {
                throw new ArgumentException("Review deadline cannot exceed 3 days (72 hours) from the generation time.");
            }
            reviewDeadlineUtc = reqUtc;
        }
        else
        {
            reviewDeadlineUtc = now.AddHours(24);
        }

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
                ReviewDeadline = reviewDeadlineUtc,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Timesheets.Add(timesheet);
            generatedTimesheets.Add(timesheet);
        }

        week.PayrollPeriod.ApprovalDeadlineAt = reviewDeadlineUtc;
        week.PayrollPeriod.UpdatedAt = now;

        var staffUserIds = staffGroups.Select(g => g.Key).ToList();
        int notificationId = 0;
        var deadlineVn = reviewDeadlineUtc + vnOffset;
        string notificationContent = $"Your timesheet for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} is ready. Please review and confirm before {deadlineVn:dd/MM/yyyy HH:mm} (UTC+7).";

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
        if (week.PayrollPeriod.Status is "SUBMITTED" or "APPROVED" or "PAID" or "CLOSED")
            throw new InvalidOperationException("Payroll period is frozen; timesheet review cannot change it.");

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

        // Vietnam Time (UTC+7)
        var vnOffset = TimeSpan.FromHours(7);

        var now = DateTime.UtcNow;

        // Auto-expire any pending timesheets whose individual deadline has passed
        foreach (var timesheet in timesheets.Where(t => string.Equals(t.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase)))
        {
            var tDeadlineUtc = timesheet.ReviewDeadline ?? (timesheet.GeneratedAt ?? week.PayrollPeriod.CreatedAt).AddHours(24);
            if (now >= tDeadlineUtc)
            {
                timesheet.StaffReviewStatus = "NO_RESPONSE";
                timesheet.UpdatedAt = now;
            }
        }

        int totalStaff = timesheets.Count;
        int confirmedStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase));
        int noResponseStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase));
        bool isAllConfirmed = (totalStaff > 0 && confirmedStaff == totalStaff);
        bool isAllResolved = (totalStaff > 0 && (confirmedStaff + noResponseStaff == totalStaff));

        if (!isAllConfirmed && !isAllResolved)
        {
            var latestPendingDeadline = timesheets
                .Where(t => string.Equals(t.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.ReviewDeadline ?? (t.GeneratedAt ?? week.PayrollPeriod.CreatedAt).AddHours(24))
                .DefaultIfEmpty()
                .Max();

            var deadlineVnDisplay = latestPendingDeadline != default
                ? (latestPendingDeadline + vnOffset).ToString("HH:mm, dd/MM/yyyy")
                : "the review deadline";

            throw new InvalidOperationException($"Cannot review timesheet: All staff must confirm their timesheet or you must wait until the review window expires ({deadlineVnDisplay}).");
        }

        foreach (var timesheet in timesheets)
        {
            if (string.Equals(timesheet.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                timesheet.StaffReviewStatus = "NO_RESPONSE";
            }
            timesheet.Status = "REVIEWED";
            timesheet.ReviewedAt = now;
            timesheet.ReviewedBy = managerId;
            timesheet.LockedAt = null;
            timesheet.LockedBy = null;
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
                TargetBranchId = null,
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
            .Include(r => r.Attendance)
                .ThenInclude(a => a.AttendanceAdjustmentHistories)
                    .ThenInclude(h => h.AdjustedByNavigation)
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
            .Include(r => r.Attendance)
                .ThenInclude(a => a.AttendancePenaltyRecords)
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
            var oldCheckIn = attendance.CheckInTime;
            var oldCheckOut = attendance.CheckOutTime;
            var oldWorkMinutes = attendance.ActualWorkingMinutes;
            var oldTotalLateMinutes = attendance.TotalLateMinutes;
            var oldChargeableLateMinutes = attendance.ChargeableLateMinutes;
            var oldTotalEarlyLeaveMinutes = attendance.TotalEarlyLeaveMinutes;
            var oldChargeableEarlyLeaveMinutes = attendance.ChargeableEarlyLeaveMinutes;

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

                // 1. Read rules and penalty definitions from RuleSnapshotJson if available
                AttendanceRuleSnapshotRoot? snapshot = null;
                if (!string.IsNullOrWhiteSpace(attendance.RuleSnapshotJson))
                {
                    try
                    {
                        snapshot = JsonSerializer.Deserialize<AttendanceRuleSnapshotRoot>(attendance.RuleSnapshotJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch
                    {
                        // Fallback to active rules below
                    }
                }

                var rule = attendance.AttendanceRule;
                int graceIn = snapshot?.AttendanceRule?.CheckInGraceMinutes ?? rule?.CheckInGraceMinutes ?? 5;
                int graceOut = snapshot?.AttendanceRule?.CheckOutGraceMinutes ?? rule?.CheckOutGraceMinutes ?? 5;

                // 2. Resolve penalty rules and penalty levels
                int? penaltyRuleId = snapshot?.AttendancePenaltyRule?.PenaltyRuleId;
                List<AttendancePenaltyLevelSnapshotItem>? penaltyLevels = snapshot?.AttendancePenaltyRule?.PenaltyLevels;

                if (penaltyRuleId == null || penaltyLevels == null)
                {
                    var shiftDateOnly = DateOnly.FromDateTime(attendance.ShiftStartAtSnapshot);
                    var activePenalty = await _context.AttendancePenaltyRules
                        .Include(r => r.AttendancePenaltyLevels)
                        .FirstOrDefaultAsync(r => r.Status == "ACTIVE" && r.EffectiveFrom <= shiftDateOnly
                            && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= shiftDateOnly), cancellationToken);

                    if (activePenalty != null)
                    {
                        penaltyRuleId = activePenalty.PenaltyRuleId;
                        penaltyLevels = activePenalty.AttendancePenaltyLevels.Select(l => new AttendancePenaltyLevelSnapshotItem
                        {
                            PenaltyLevelId = l.PenaltyLevelId,
                            ViolationType = l.ViolationType,
                            FromMinutes = l.FromMinutes,
                            ToMinutes = l.ToMinutes,
                            PenaltyAmount = l.PenaltyAmount
                        }).ToList();
                    }
                }

                // 3. Recalculate late minutes
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

                // 4. Recalculate early leave minutes
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

                // 5. Update Status
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

                // 6. Synchronize AttendancePenaltyRecords
                // A. Handle LATE penalty records
                var activeLateRecords = attendance.AttendancePenaltyRecords
                    .Where(p => string.Equals(p.ViolationType, "LATE", StringComparison.OrdinalIgnoreCase) && p.Status == "APPLIED")
                    .ToList();

                if (attendance.ChargeableLateMinutes > 0 && penaltyRuleId.HasValue && penaltyLevels != null)
                {
                    var matchedLateLevel = penaltyLevels
                        .FirstOrDefault(l => string.Equals(l.ViolationType, "LATE", StringComparison.OrdinalIgnoreCase) &&
                                             attendance.ChargeableLateMinutes >= l.FromMinutes &&
                                             (!l.ToMinutes.HasValue || attendance.ChargeableLateMinutes <= l.ToMinutes.Value));

                    if (matchedLateLevel != null)
                    {
                        var primaryLate = activeLateRecords.FirstOrDefault();
                        // If already had an active penalty and minutes/amount changed -> update in-place to avoid unique index violation
                        if (primaryLate != null)
                        {
                            primaryLate.ViolationMinutes = attendance.ChargeableLateMinutes;
                            primaryLate.PenaltyLevelId = matchedLateLevel.PenaltyLevelId;
                            primaryLate.PenaltyRuleId = penaltyRuleId.Value;
                            primaryLate.PenaltyAmount = matchedLateLevel.PenaltyAmount;
                            primaryLate.CreatedAt = now;

                            foreach (var extra in activeLateRecords.Skip(1))
                            {
                                extra.Status = "CANCELLED";
                            }
                        }
                        else
                        {
                            var newRecord = new AttendancePenaltyRecord
                            {
                                AttendanceId = attendance.AttendanceId,
                                PenaltyRuleId = penaltyRuleId.Value,
                                PenaltyLevelId = matchedLateLevel.PenaltyLevelId,
                                ViolationType = "LATE",
                                ViolationMinutes = attendance.ChargeableLateMinutes,
                                PenaltyAmount = matchedLateLevel.PenaltyAmount,
                                Status = "APPLIED",
                                CreatedAt = now
                            };
                            attendance.AttendancePenaltyRecords.Add(newRecord);
                        }
                    }
                    else
                    {
                        // Chargeable late is below the lowest penalty threshold -> cancel old records
                        foreach (var oldLate in activeLateRecords)
                        {
                            oldLate.Status = "CANCELLED";
                        }
                    }
                }
                else
                {
                    // No chargeable late minutes -> cancel all active late records
                    foreach (var oldLate in activeLateRecords)
                    {
                        oldLate.Status = "CANCELLED";
                    }
                }

                // B. Handle EARLY_LEAVE penalty records
                var activeEarlyRecords = attendance.AttendancePenaltyRecords
                    .Where(p => string.Equals(p.ViolationType, "EARLY_LEAVE", StringComparison.OrdinalIgnoreCase) && p.Status == "APPLIED")
                    .ToList();

                if (attendance.ChargeableEarlyLeaveMinutes > 0 && penaltyRuleId.HasValue && penaltyLevels != null)
                {
                    var matchedEarlyLevel = penaltyLevels
                        .FirstOrDefault(l => string.Equals(l.ViolationType, "EARLY_LEAVE", StringComparison.OrdinalIgnoreCase) &&
                                             attendance.ChargeableEarlyLeaveMinutes >= l.FromMinutes &&
                                             (!l.ToMinutes.HasValue || attendance.ChargeableEarlyLeaveMinutes <= l.ToMinutes.Value));

                    if (matchedEarlyLevel != null)
                    {
                        var primaryEarly = activeEarlyRecords.FirstOrDefault();
                        if (primaryEarly != null)
                        {
                            primaryEarly.ViolationMinutes = attendance.ChargeableEarlyLeaveMinutes;
                            primaryEarly.PenaltyLevelId = matchedEarlyLevel.PenaltyLevelId;
                            primaryEarly.PenaltyRuleId = penaltyRuleId.Value;
                            primaryEarly.PenaltyAmount = matchedEarlyLevel.PenaltyAmount;
                            primaryEarly.CreatedAt = now;

                            foreach (var extra in activeEarlyRecords.Skip(1))
                            {
                                extra.Status = "CANCELLED";
                            }
                        }
                        else
                        {
                            var newRecord = new AttendancePenaltyRecord
                            {
                                AttendanceId = attendance.AttendanceId,
                                PenaltyRuleId = penaltyRuleId.Value,
                                PenaltyLevelId = matchedEarlyLevel.PenaltyLevelId,
                                ViolationType = "EARLY_LEAVE",
                                ViolationMinutes = attendance.ChargeableEarlyLeaveMinutes,
                                PenaltyAmount = matchedEarlyLevel.PenaltyAmount,
                                Status = "APPLIED",
                                CreatedAt = now
                            };
                            attendance.AttendancePenaltyRecords.Add(newRecord);
                        }
                    }
                    else
                    {
                        foreach (var oldEarly in activeEarlyRecords)
                        {
                            oldEarly.Status = "CANCELLED";
                        }
                    }
                }
                else
                {
                    foreach (var oldEarly in activeEarlyRecords)
                    {
                        oldEarly.Status = "CANCELLED";
                    }
                }

                if (attendance.Assignment != null)
                {
                    attendance.Assignment.Status = "COMPLETED";
                    attendance.Assignment.UpdatedAt = now;
                }

                attendance.UpdatedAt = now;
            }

            var historyRecord = new AttendanceAdjustmentHistory
            {
                AttendanceId = attendance.AttendanceId,
                AdjustedBy = managerId,
                AdjustmentSource = "STAFF_REQUEST",
                Reason = !string.IsNullOrWhiteSpace(request.ManagerNote)
                    ? request.ManagerNote
                    : (!string.IsNullOrWhiteSpace(request.Reason) ? request.Reason : "Timesheet adjustment request approved"),
                OldCheckInTime = oldCheckIn,
                NewCheckInTime = attendance.CheckInTime,
                OldCheckOutTime = oldCheckOut,
                NewCheckOutTime = attendance.CheckOutTime,
                OldActualWorkingMinutes = oldWorkMinutes,
                NewActualWorkingMinutes = attendance.ActualWorkingMinutes,
                OldTotalLateMinutes = oldTotalLateMinutes,
                NewTotalLateMinutes = attendance.TotalLateMinutes,
                OldChargeableLateMinutes = oldChargeableLateMinutes,
                NewChargeableLateMinutes = attendance.ChargeableLateMinutes,
                OldTotalEarlyLeaveMinutes = oldTotalEarlyLeaveMinutes,
                NewTotalEarlyLeaveMinutes = attendance.TotalEarlyLeaveMinutes,
                OldChargeableEarlyLeaveMinutes = oldChargeableEarlyLeaveMinutes,
                NewChargeableEarlyLeaveMinutes = attendance.ChargeableEarlyLeaveMinutes,
                AdjustedAt = now
            };
            _context.AttendanceAdjustmentHistories.Add(historyRecord);
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
                if (timesheet.Status == "REVIEWED") { timesheet.ReviewedAt = now; timesheet.ReviewedBy = managerId; }
            timesheet.UpdatedAt = now;
            }

            var otherPendingAdjustments = await _context.TimesheetAdjustmentRequests
                .AnyAsync(r => r.TimesheetId == timesheet.TimesheetId && r.AdjustmentRequestId != adjustmentRequestId && r.Status == "PENDING", cancellationToken);

            if (!otherPendingAdjustments)
            {
                timesheet.StaffReviewStatus = "PENDING";
                timesheet.StaffReviewedAt = null;
                timesheet.StaffReviewNote = null;
                timesheet.UpdatedAt = now;
            }
        }

        // Create In-App Notification in DB for Staff
        var shiftName = attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
        var shiftDate = attendance?.ShiftStartAtSnapshot.ToString("dd/MM/yyyy") ?? "";
        var notif = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = "Timesheet Adjustment Approved",
            Content = $"Your adjustment request for shift '{shiftName}' on {shiftDate} has been approved by the manager. Actual attendance hours have been updated. Please review your timesheet and confirm.",
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

        var timesheet = request.Timesheet;
        if (timesheet != null)
        {
            var otherPendingAdjustments = await _context.TimesheetAdjustmentRequests
                .AnyAsync(r => r.TimesheetId == timesheet.TimesheetId && r.AdjustmentRequestId != adjustmentRequestId && r.Status == "PENDING", cancellationToken);

            if (!otherPendingAdjustments)
            {
                timesheet.StaffReviewStatus = "PENDING";
                timesheet.StaffReviewedAt = null;
                timesheet.StaffReviewNote = null;
                timesheet.UpdatedAt = now;
            }
        }

        // Create In-App Notification in DB for Staff
        var shiftName = request.Attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
        var shiftDate = request.Attendance?.ShiftStartAtSnapshot.ToString("dd/MM/yyyy") ?? "";
        var rejectNotif = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = "Timesheet Adjustment Rejected",
            Content = $"Your adjustment request for shift '{shiftName}' on {shiftDate} was rejected by the manager (Reason: {managerNote.Trim()}). Please review your timesheet and confirm.",
            NotificationType = "TIMESHEET",
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = "Custom",
            Channels = new List<string> { "InApp" },
            TargetRoles = new List<string> { "STAFF" }
        };

        rejectNotif.UserNotifications.Add(new UserNotification
        {
            UserId = request.UserId,
            IsRead = false,
            CreatedAt = now
        });

        _context.Notifications.Add(rejectNotif);
        await _context.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task<List<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)>> AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

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

            // Find timesheets whose review deadline has passed and staff is still PENDING
            var expiredStaffTimesheets = timesheets
                .Where(t => t.Status == "GENERATED" &&
                            string.Equals(t.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                            (t.ReviewDeadline.HasValue ? now >= t.ReviewDeadline.Value : now >= (t.GeneratedAt ?? week.PayrollPeriod!.CreatedAt).AddHours(24)))
                .ToList();

            if (!expiredStaffTimesheets.Any())
                continue;

            // Mark expired staff as NO_RESPONSE (Keep Status as GENERATED for manager review)
            foreach (var t in expiredStaffTimesheets)
            {
                t.StaffReviewStatus = "NO_RESPONSE";
                t.UpdatedAt = now;
            }

            var staffUserIds = expiredStaffTimesheets.Select(t => t.UserId).Distinct().ToList();
            int notificationId = 0;
            string notificationContent = $"The review deadline for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has expired. Your timesheet has been marked as No Response.";

            if (staffUserIds.Count > 0)
            {
                var creatorId = week.PayrollPeriod!.CreatedBy;
                var notification = new WorkShiftManagement.DAL.Entities.Notification
                {
                    Title = "Timesheet Review Window Expired",
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

                // Also notify Branch Managers that the deadline has passed
                var branchId = week.BranchId;
                var managerIds = await _context.Users
                    .Where(u => u.BranchId == branchId && u.Role.RoleName == "MANAGER" && u.Status == "ACTIVE")
                    .Select(u => u.UserId)
                    .ToListAsync(cancellationToken);

                if (managerIds.Any())
                {
                    var managerNotif = new WorkShiftManagement.DAL.Entities.Notification
                    {
                        Title = "Timesheet Review Window Expired",
                        Content = $"Review deadline for week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy} has ended. Timesheets are ready for manager review.",
                        NotificationType = "TIMESHEET",
                        CreatedBy = creatorId,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsDeleted = false,
                        TargetType = "Custom",
                        Channels = new List<string> { "InApp" },
                        TargetRoles = new List<string> { "MANAGER" }
                    };

                    foreach (var mId in managerIds)
                    {
                        managerNotif.UserNotifications.Add(new UserNotification
                        {
                            UserId = mId,
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                    _context.Notifications.Add(managerNotif);
                }

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);
                notificationId = notification.NotificationId;
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            results.Add((week, expiredStaffTimesheets, staffUserIds, notificationId, notificationContent));
        }

        return results;
    }

    public async Task<(Timesheet Timesheet, int ScheduleWeekId, Guid StaffUserId, int NotificationId, string NotificationContent)> ReopenStaffTimesheetAsync(
        int timesheetId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.User)
            .Include(t => t.PayrollPeriod)
                .ThenInclude(p => p!.ScheduleWeek)
                    .ThenInclude(w => w!.Branch)
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId, cancellationToken);

        if (timesheet == null)
        {
            throw new KeyNotFoundException($"Timesheet with ID {timesheetId} was not found.");
        }

        var week = timesheet.PayrollPeriod?.ScheduleWeek;
        if (week == null || week.BranchId != branchId)
        {
            throw new UnauthorizedAccessException("You are not authorized to modify timesheets outside your branch.");
        }

        if (string.Equals(timesheet.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timesheet.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(week.PayrollPeriod?.Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(week.PayrollPeriod?.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(week.PayrollPeriod?.Status, "PAID", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(week.PayrollPeriod?.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot reopen review: The weekly timesheet has already been reviewed or locked.");
        }

        if (string.Equals(timesheet.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot reopen review: Staff has already confirmed this timesheet.");
        }

        if (string.Equals(timesheet.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Timesheet review is already open (PENDING) for this staff member.");
        }

        if (!string.Equals(timesheet.StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot reopen review: Only timesheets with 'NO_RESPONSE' status can be reopened (current: '{timesheet.StaffReviewStatus}').");
        }

        var now = DateTime.UtcNow;
        var vnOffset = TimeSpan.FromHours(7);
        var newDeadlineUtc = now.AddHours(24);

        timesheet.StaffReviewStatus = "PENDING";
        timesheet.ReviewDeadline = newDeadlineUtc;
        timesheet.StaffReviewedAt = null;
        timesheet.StaffReviewNote = null;
        timesheet.UpdatedAt = now;

        if (week.PayrollPeriod != null && (!week.PayrollPeriod.ApprovalDeadlineAt.HasValue || week.PayrollPeriod.ApprovalDeadlineAt.Value < newDeadlineUtc))
        {
            week.PayrollPeriod.ApprovalDeadlineAt = newDeadlineUtc;
            week.PayrollPeriod.UpdatedAt = now;
        }

        var deadlineVn = newDeadlineUtc + vnOffset;
        string notificationContent = $"The review window for your timesheet (week {week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy}) has been reopened by your manager. New deadline is {deadlineVn:dd/MM/yyyy HH:mm} (UTC+7). Please review and confirm.";

        var notification = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = "Weekly Timesheet Review Window Reopened",
            Content = notificationContent,
            NotificationType = "TIMESHEET",
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = "Custom",
            Channels = new List<string> { "InApp" },
            TargetRoles = new List<string> { "STAFF" }
        };

        notification.UserNotifications.Add(new UserNotification
        {
            UserId = timesheet.UserId,
            IsRead = false,
            CreatedAt = now
        });

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return (timesheet, week.ScheduleWeekId, timesheet.UserId, notification.NotificationId, notificationContent);
    }
}
