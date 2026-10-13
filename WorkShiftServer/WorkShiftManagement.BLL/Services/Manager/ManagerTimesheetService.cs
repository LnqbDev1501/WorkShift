using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ManagerTimesheetService : IManagerTimesheetService
{
    private readonly IManagerTimesheetRepo _timesheetRepo;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly ILogger<ManagerTimesheetService> _logger;

    public ManagerTimesheetService(
        IManagerTimesheetRepo timesheetRepo,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<ManagerTimesheetService> logger)
    {
        _timesheetRepo = timesheetRepo;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task<ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>> GetWeeklyTimesheetsAsync(
        Guid managerId,
        ManagerTimesheetListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        ArgumentNullException.ThrowIfNull(query);

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        if (query.BranchId.HasValue && query.BranchId.Value != branchId && manager.Role.RoleName != "ADMIN")
        {
            throw new UnauthorizedAccessException("You are only authorized to view timesheets for your assigned branch.");
        }

        int targetBranchId = query.BranchId ?? branchId;

        DateOnly? fromDate = query.FromDate;
        DateOnly? toDate = query.ToDate;

        if (query.Month.HasValue && query.Year.HasValue)
        {
            fromDate = new DateOnly(query.Year.Value, query.Month.Value, 1);
            toDate = fromDate.Value.AddMonths(1).AddDays(-1);
        }

        int pageIndex = Math.Max(1, query.PageIndex);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (weeks, totalCount) = await _timesheetRepo.GetWeeklyTimesheetsAsync(
            targetBranchId,
            query.Search,
            query.ScheduleWeekId,
            fromDate,
            toDate,
            query.Status,
            pageIndex,
            pageSize,
            cancellationToken);

        var availableWeeks = await _timesheetRepo.GetAvailableWeeksForBranchAsync(targetBranchId, cancellationToken);
        var availablePeriods = availableWeeks.Select(w => new ManagerTimesheetPeriodOptionDto
        {
            ScheduleWeekId = w.ScheduleWeekId,
            Period = $"{w.WeekStartDate:dd/MM} - {w.WeekEndDate:dd/MM}",
            WeekStartDate = w.WeekStartDate,
            WeekEndDate = w.WeekEndDate
        }).ToList();

        var items = weeks.Select(MapWeeklySummary).ToList();

        return new ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>
        {
            Items = items,
            AvailablePeriods = availablePeriods,
            TotalItems = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    private static (int TotalStaff, int ConfirmedStaff, int TotalWorkMinutes, int TotalLateMinutes, int TotalEarlyLeaveMinutes, string Status)
        ComputeSummaryMetrics(WorkScheduleWeek week, List<Timesheet> timesheets)
    {
        if (timesheets.Count > 0)
        {
            int totalStaff = timesheets.Count;
            int confirmedStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase));
            int totalWorkMinutes = timesheets.Sum(t => t.TotalWorkMinutes);
            int totalLateMinutes = timesheets.Sum(t => t.TotalLateMinutes);
            int totalEarlyLeaveMinutes = timesheets.Sum(t => t.TotalEarlyLeaveMinutes);

            string status = timesheets.All(t => string.Equals(t.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
                ? "LOCKED"
                : (!string.IsNullOrWhiteSpace(week.PayrollPeriod?.Status) ? week.PayrollPeriod.Status : "GENERATED");

            return (totalStaff, confirmedStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status);
        }

        int ungeneratedStaff = (week.WorkShifts ?? Enumerable.Empty<WorkShift>())
            .SelectMany(s => s.ShiftAssignments ?? Enumerable.Empty<ShiftAssignment>())
            .Where(a => a.Status != "CANCELLED")
            .Select(a => a.UserId)
            .Distinct()
            .Count();

        return (ungeneratedStaff, 0, 0, 0, 0, "NOT GENERATED");
    }

    private static ManagerWeeklyTimesheetSummaryResponse MapWeeklySummary(WorkScheduleWeek week)
    {
        var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
        var (totalStaff, confirmedStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status) =
            ComputeSummaryMetrics(week, timesheets);

        string staffReview = status == "NOT GENERATED"
            ? "-"
            : $"{confirmedStaff}/{totalStaff} Confirmed";

        decimal confirmationRate = (status != "NOT GENERATED" && totalStaff > 0)
            ? Math.Round((decimal)confirmedStaff / totalStaff * 100m, 2)
            : 0m;

        decimal totalWorkHours = Math.Round((decimal)totalWorkMinutes / 60m, 2);

        return new ManagerWeeklyTimesheetSummaryResponse
        {
            ScheduleWeekId = week.ScheduleWeekId,
            PayrollPeriodId = week.PayrollPeriod?.PayrollPeriodId,
            Period = $"{week.WeekStartDate:dd/MM} - {week.WeekEndDate:dd/MM}",
            WeekStartDate = week.WeekStartDate,
            WeekEndDate = week.WeekEndDate,
            BranchId = week.BranchId,
            BranchName = week.Branch?.BranchName ?? $"Branch {week.BranchId}",
            Status = status,
            TotalStaff = totalStaff,
            ConfirmedStaff = confirmedStaff,
            StaffReview = staffReview,
            ConfirmationRate = confirmationRate,
            TotalWorkHours = totalWorkHours,
            TotalWorkMinutes = totalWorkMinutes,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
            IsLocked = string.Equals(status, "LOCKED", StringComparison.OrdinalIgnoreCase)
        };
    }

    public async Task<ManagerWeeklyTimesheetDetailResponse?> GetWeeklyTimesheetDetailAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        if (scheduleWeekId <= 0)
        {
            throw new ArgumentException("Valid schedule week ID is required.");
        }

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var week = await _timesheetRepo.GetWeeklyTimesheetDetailAsync(scheduleWeekId, branchId, cancellationToken);
        if (week == null)
        {
            return null;
        }

        var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
        var (totalStaff, confirmedStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status) =
            ComputeSummaryMetrics(week, timesheets);

        var staffItems = new List<ManagerStaffTimesheetItemDto>();

        var assignmentsByUser = (week.WorkShifts ?? Enumerable.Empty<WorkShift>())
            .SelectMany(s => (s.ShiftAssignments ?? Enumerable.Empty<ShiftAssignment>()).Select(a => new { Shift = s, Assignment = a }))
            .Where(x => x.Assignment.Status != "CANCELLED")
            .GroupBy(x => x.Assignment.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Shift.ScheduledStartAt).ToList());

        List<ManagerStaffShiftAttendanceDto> MapUserShifts(Guid userId)
        {
            if (!assignmentsByUser.TryGetValue(userId, out var userItems))
                return new List<ManagerStaffShiftAttendanceDto>();

            return userItems.Select(x =>
            {
                var att = x.Assignment.Attendance;
                string attStatus = att?.Status
                    ?? (DateTime.UtcNow > x.Shift.ScheduledEndAt ? "ABSENT" : "SCHEDULED");

                return new ManagerStaffShiftAttendanceDto
                {
                    AssignmentId = x.Assignment.AssignmentId,
                    ShiftId = x.Shift.ShiftId,
                    ShiftName = x.Shift.ShiftNameSnapshot,
                    ScheduledStartAt = x.Shift.ScheduledStartAt,
                    ScheduledEndAt = x.Shift.ScheduledEndAt,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    ActualWorkingMinutes = att?.ActualWorkingMinutes ?? 0,
                    TotalLateMinutes = att?.TotalLateMinutes ?? 0,
                    TotalEarlyLeaveMinutes = att?.TotalEarlyLeaveMinutes ?? 0,
                    AttendanceStatus = attStatus,
                    AssignmentStatus = x.Assignment.Status
                };
            }).ToList();
        }

        if (timesheets.Count > 0)
        {
            foreach (var t in timesheets.OrderBy(t => t.User.FullName))
            {
                staffItems.Add(new ManagerStaffTimesheetItemDto
                {
                    TimesheetId = t.TimesheetId,
                    UserId = t.UserId,
                    FullName = t.User.FullName,
                    Email = t.User.Email,
                    Phone = t.User.Phone,
                    RoleName = t.User.Role?.RoleName,
                    TotalAssignedShifts = t.TotalAssignedShifts,
                    CompletedShifts = t.CompletedShiftCount,
                    TotalWorkHours = Math.Round((decimal)t.TotalWorkMinutes / 60m, 2),
                    TotalWorkMinutes = t.TotalWorkMinutes,
                    TotalLateMinutes = t.TotalLateMinutes,
                    TotalEarlyLeaveMinutes = t.TotalEarlyLeaveMinutes,
                    StaffReviewStatus = t.StaffReviewStatus ?? "PENDING",
                    Status = t.Status ?? status,
                    Shifts = MapUserShifts(t.UserId)
                });
            }
        }
        else
        {
            var ungeneratedUsers = assignmentsByUser.Values
                .Select(list => list[0].Assignment.User)
                .Where(u => u != null)
                .OrderBy(u => u.FullName);

            foreach (var u in ungeneratedUsers)
            {
                staffItems.Add(new ManagerStaffTimesheetItemDto
                {
                    TimesheetId = null,
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleName = u.Role?.RoleName,
                    TotalAssignedShifts = assignmentsByUser[u.UserId].Count,
                    CompletedShifts = 0,
                    TotalWorkHours = 0m,
                    TotalWorkMinutes = 0,
                    TotalLateMinutes = 0,
                    TotalEarlyLeaveMinutes = 0,
                    StaffReviewStatus = "-",
                    Status = "NOT GENERATED",
                    Shifts = MapUserShifts(u.UserId)
                });
            }
        }

        string staffReview = status == "NOT GENERATED"
            ? "-"
            : $"{confirmedStaff}/{totalStaff} Confirmed";

        decimal confirmationRate = (status != "NOT GENERATED" && totalStaff > 0)
            ? Math.Round((decimal)confirmedStaff / totalStaff * 100m, 2)
            : 0m;

        decimal totalWorkHours = Math.Round((decimal)totalWorkMinutes / 60m, 2);

        return new ManagerWeeklyTimesheetDetailResponse
        {
            ScheduleWeekId = week.ScheduleWeekId,
            PayrollPeriodId = week.PayrollPeriod?.PayrollPeriodId,
            Period = $"{week.WeekStartDate:dd/MM} - {week.WeekEndDate:dd/MM}",
            WeekStartDate = week.WeekStartDate,
            WeekEndDate = week.WeekEndDate,
            BranchId = week.BranchId,
            BranchName = week.Branch?.BranchName ?? $"Branch {week.BranchId}",
            Status = status,
            TotalStaff = totalStaff,
            ConfirmedStaff = confirmedStaff,
            StaffReview = staffReview,
            ConfirmationRate = confirmationRate,
            TotalWorkHours = totalWorkHours,
            TotalWorkMinutes = totalWorkMinutes,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
            StaffTimesheets = staffItems
        };
    }

    public async Task<ManagerWeeklyTimesheetDetailResponse> GenerateWeeklyTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        if (scheduleWeekId <= 0)
        {
            throw new ArgumentException("Schedule week ID must be greater than zero.");
        }

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var (_, _, staffUserIds, notificationId, notificationContent) =
            await _timesheetRepo.GenerateWeeklyTimesheetAsync(scheduleWeekId, branchId, managerId, cancellationToken);

        if (notificationId > 0 && staffUserIds.Any())
        {
            foreach (var staffId in staffUserIds)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(staffId, new RealtimeNotificationResponse
                    {
                        NotificationId = notificationId,
                        Title = "Weekly Timesheet Generated",
                        Message = notificationContent,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish realtime notification to staff member {StaffId}", staffId);
                }
            }
        }

        var detail = await GetWeeklyTimesheetDetailAsync(managerId, scheduleWeekId, cancellationToken);
        return detail ?? throw new InvalidOperationException("Timesheet details could not be retrieved after generation.");
    }
}
