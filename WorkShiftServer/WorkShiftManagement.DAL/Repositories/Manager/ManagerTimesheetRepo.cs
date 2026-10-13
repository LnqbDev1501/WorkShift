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
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
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
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.User)
                        .ThenInclude(u => u.Role)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.Attendance)
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
}
