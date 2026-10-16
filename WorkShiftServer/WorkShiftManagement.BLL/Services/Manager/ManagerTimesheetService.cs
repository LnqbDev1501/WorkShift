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

            string status = timesheets.All(t => string.Equals(t.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase))
                ? "REVIEWED"
                : (timesheets.All(t => string.Equals(t.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
                    ? "LOCKED"
                    : (!string.IsNullOrWhiteSpace(week.PayrollPeriod?.Status) ? week.PayrollPeriod.Status : "GENERATED"));

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
            IsLocked = string.Equals(status, "LOCKED", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "REVIEWED", StringComparison.OrdinalIgnoreCase)
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

                var approvedLeave = x.Assignment.LeaveRequestAssignments
                    ?.Select(l => l.Request)
                    ?.FirstOrDefault(r => r != null && string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

                bool isExcusedLeave = approvedLeave != null || string.Equals(attStatus, "LEAVE", StringComparison.OrdinalIgnoreCase);
                string? leaveReason = approvedLeave?.Reason;

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
                    AttendanceStatus = isExcusedLeave ? "LEAVE" : attStatus,
                    AssignmentStatus = x.Assignment.Status,
                    IsExcusedLeave = isExcusedLeave,
                    LeaveReason = leaveReason
                };
            }).ToList();
        }

        if (timesheets.Count > 0)
        {
            foreach (var t in timesheets.OrderBy(t => t.User.FullName))
            {
                var userShifts = MapUserShifts(t.UserId);
                int lateCount = userShifts.Count(s => s.TotalLateMinutes > 0);
                int earlyLeaveCount = userShifts.Count(s => s.TotalEarlyLeaveMinutes > 0);
                int excusedLeaveCount = userShifts.Count(s => s.IsExcusedLeave);
                int unexcusedAbsentCount = userShifts.Count(s => !s.IsExcusedLeave && (s.AttendanceStatus == "ABSENT" || (s.CheckInTime == null && DateTime.UtcNow > s.ScheduledEndAt)));

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
                    LateCount = lateCount,
                    EarlyLeaveCount = earlyLeaveCount,
                    ExcusedLeaveCount = excusedLeaveCount,
                    UnexcusedAbsentCount = unexcusedAbsentCount,
                    StaffReviewStatus = t.StaffReviewStatus ?? "PENDING",
                    Status = t.Status ?? status,
                    Shifts = userShifts
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
                var userShifts = MapUserShifts(u.UserId);
                int lateCount = userShifts.Count(s => s.TotalLateMinutes > 0);
                int earlyLeaveCount = userShifts.Count(s => s.TotalEarlyLeaveMinutes > 0);
                int excusedLeaveCount = userShifts.Count(s => s.IsExcusedLeave);
                int unexcusedAbsentCount = userShifts.Count(s => !s.IsExcusedLeave && (s.AttendanceStatus == "ABSENT" || (s.CheckInTime == null && DateTime.UtcNow > s.ScheduledEndAt)));

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
                    LateCount = lateCount,
                    EarlyLeaveCount = earlyLeaveCount,
                    ExcusedLeaveCount = excusedLeaveCount,
                    UnexcusedAbsentCount = unexcusedAbsentCount,
                    StaffReviewStatus = "-",
                    Status = "NOT GENERATED",
                    Shifts = userShifts
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

        int pendingAdjustmentCount = timesheets
            .SelectMany(t => t.TimesheetAdjustmentRequests ?? Enumerable.Empty<TimesheetAdjustmentRequest>())
            .Count(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

        DateTime? reviewDeadline = null;
        bool isPastDeadline = false;
        bool canLockTimesheet = false;
        string? lockEligibilityMessage = null;

        if (status != "NOT GENERATED" && status != "LOCKED" && status != "REVIEWED" && timesheets.Any())
        {
            var firstGeneratedAt = timesheets.Where(t => t.GeneratedAt.HasValue).Min(t => t.GeneratedAt) 
                ?? week.PayrollPeriod?.CreatedAt 
                ?? DateTime.UtcNow;

            var vnOffset = TimeSpan.FromHours(7);
            var generatedAtVn = firstGeneratedAt + vnOffset;
            var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
            reviewDeadline = week.PayrollPeriod?.ApprovalDeadlineAt ?? (endOfDayVn - vnOffset);

            isPastDeadline = DateTime.UtcNow >= reviewDeadline.Value;
            bool isAllConfirmed = (totalStaff > 0 && confirmedStaff == totalStaff);

            if (pendingAdjustmentCount > 0)
            {
                canLockTimesheet = false;
                lockEligibilityMessage = $"Cannot review: {pendingAdjustmentCount} pending adjustment request(s) must be resolved first.";
            }
            else if (isAllConfirmed)
            {
                canLockTimesheet = true;
                lockEligibilityMessage = "Ready to review: 100% staff members have confirmed.";
            }
            else if (isPastDeadline)
            {
                canLockTimesheet = true;
                lockEligibilityMessage = "Ready to review: Review deadline has expired at end of day.";
            }
            else
            {
                canLockTimesheet = false;
                var deadlineVn = reviewDeadline.Value + TimeSpan.FromHours(7);
                lockEligibilityMessage = $"Waiting for 100% staff confirmation ({confirmedStaff}/{totalStaff} confirmed) or wait until end of day ({deadlineVn:HH:mm dd/MM/yyyy}).";
            }
        }
        else if (status == "REVIEWED" || status == "LOCKED")
        {
            canLockTimesheet = false;
            lockEligibilityMessage = "Timesheet has been reviewed.";
        }
        else
        {
            canLockTimesheet = false;
            lockEligibilityMessage = "Timesheet has not been generated yet.";
        }

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
            CanLockTimesheet = canLockTimesheet,
            ReviewDeadline = reviewDeadline,
            IsPastDeadline = isPastDeadline,
            PendingAdjustmentCount = pendingAdjustmentCount,
            LockEligibilityMessage = lockEligibilityMessage,
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

    public async Task<ManagerWeeklyTimesheetDetailResponse> LockWeeklyTimesheetAsync(
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

        var (_, _, staffUserIds, accountantUserIds, notificationId, accountantNotificationId, notificationContent, accountantNotificationContent) =
            await _timesheetRepo.LockWeeklyTimesheetAsync(scheduleWeekId, branchId, managerId, cancellationToken);

        if (notificationId > 0 && staffUserIds.Any())
        {
            foreach (var staffId in staffUserIds)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(staffId, new RealtimeNotificationResponse
                    {
                        NotificationId = notificationId,
                        Title = "Weekly Timesheet Reviewed",
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

        if (accountantNotificationId > 0 && accountantUserIds.Any())
        {
            foreach (var accId in accountantUserIds)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(accId, new RealtimeNotificationResponse
                    {
                        NotificationId = accountantNotificationId,
                        Title = "Weekly Timesheet Reviewed",
                        Message = accountantNotificationContent,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish realtime notification to accountant {AccountantId}", accId);
                }
            }
        }

        var detail = await GetWeeklyTimesheetDetailAsync(managerId, scheduleWeekId, cancellationToken);
        return detail ?? throw new InvalidOperationException("Timesheet details could not be retrieved after reviewing.");
    }

    public async Task<ManagerTimesheetAdjustmentPagedResponse> GetTimesheetAdjustmentsAsync(
        Guid managerId,
        ManagerTimesheetAdjustmentListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
            throw new ArgumentException("Authenticated manager identity is required.");

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        int pageIndex = Math.Max(1, query.PageIndex);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount, pendingCount, approvedCount, rejectedCount) =
            await _timesheetRepo.GetTimesheetAdjustmentsAsync(
                branchId,
                query.Search,
                query.Status,
                query.ScheduleWeekId,
                query.StaffId,
                pageIndex,
                pageSize,
                cancellationToken);

        return new ManagerTimesheetAdjustmentPagedResponse
        {
            Items = items.Select(MapToAdjustmentDto).ToList(),
            TotalCount = totalCount,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<ManagerTimesheetAdjustmentDto?> GetAdjustmentDetailAsync(
        Guid managerId,
        int adjustmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
            throw new ArgumentException("Authenticated manager identity is required.");

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var request = await _timesheetRepo.GetAdjustmentDetailAsync(adjustmentRequestId, branchId, cancellationToken);
        return request != null ? MapToAdjustmentDto(request) : null;
    }

    public async Task<ManagerTimesheetAdjustmentDto> ApproveAdjustmentRequestAsync(
        Guid managerId,
        int adjustmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
            throw new ArgumentException("Authenticated manager identity is required.");

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var approved = await _timesheetRepo.ApproveAdjustmentRequestAsync(
            adjustmentRequestId, branchId, managerId, cancellationToken);

        try
        {
            var shiftName = approved.Attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
            await _realtimePublisher.PublishAsync(approved.UserId, new RealtimeNotificationResponse
            {
                Title = "Timesheet Adjustment Approved",
                Message = $"Your adjustment request for shift '{shiftName}' has been approved.",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish realtime notification to staff member {StaffId}", approved.UserId);
        }

        return MapToAdjustmentDto(approved);
    }

    public async Task<ManagerTimesheetAdjustmentDto> RejectAdjustmentRequestAsync(
        Guid managerId,
        int adjustmentRequestId,
        RejectAdjustmentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
            throw new ArgumentException("Authenticated manager identity is required.");

        if (string.IsNullOrWhiteSpace(dto?.ManagerNote))
            throw new ArgumentException("Rejection note is required.");

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var rejected = await _timesheetRepo.RejectAdjustmentRequestAsync(
            adjustmentRequestId, branchId, managerId, dto.ManagerNote.Trim(), cancellationToken);

        try
        {
            var shiftName = rejected.Attendance?.Assignment?.Shift?.ShiftNameSnapshot ?? "Shift";
            await _realtimePublisher.PublishAsync(rejected.UserId, new RealtimeNotificationResponse
            {
                Title = "Timesheet Adjustment Rejected",
                Message = $"Your adjustment request for shift '{shiftName}' has been rejected: {dto.ManagerNote.Trim()}",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish realtime notification to staff member {StaffId}", rejected.UserId);
        }

        return MapToAdjustmentDto(rejected);
    }

    private static ManagerTimesheetAdjustmentDto MapToAdjustmentDto(TimesheetAdjustmentRequest r)
    {
        var shift = r.Attendance?.Assignment?.Shift;
        var week = r.Timesheet?.PayrollPeriod?.ScheduleWeek;
        var period = week != null ? $"{week.WeekStartDate:dd/MM} - {week.WeekEndDate:dd/MM}" : "";

        return new ManagerTimesheetAdjustmentDto
        {
            AdjustmentRequestId = r.AdjustmentRequestId,
            TimesheetId = r.TimesheetId,
            ScheduleWeekId = week?.ScheduleWeekId ?? r.Timesheet?.PayrollPeriod?.ScheduleWeekId ?? 0,
            Period = period,
            StaffId = r.UserId,
            StaffName = r.User?.FullName ?? "Unknown",
            StaffEmail = r.User?.Email,
            StaffPhone = r.User?.Phone,
            RoleName = r.User?.Role?.RoleName ?? "Staff",
            ShiftId = shift?.ShiftId ?? 0,
            ShiftName = shift?.ShiftNameSnapshot ?? "Shift",
            ScheduledStartAt = shift?.ScheduledStartAt ?? r.CreatedAt,
            ScheduledEndAt = shift?.ScheduledEndAt ?? r.CreatedAt,
            OriginalCheckInTime = r.CurrentCheckInSnapshot ?? r.Attendance?.CheckInTime,
            OriginalCheckOutTime = r.CurrentCheckOutSnapshot ?? r.Attendance?.CheckOutTime,
            AdjustCheckIn = r.AdjustCheckIn,
            RequestedCheckInTime = r.RequestedCheckInTime,
            AdjustCheckOut = r.AdjustCheckOut,
            RequestedCheckOutTime = r.RequestedCheckOutTime,
            Reason = r.Reason,
            Status = r.Status,
            ManagerNote = r.ManagerNote,
            ReviewedBy = r.ReviewedBy,
            ReviewedByName = r.ReviewedByNavigation?.FullName,
            ReviewedAt = r.ReviewedAt,
            CreatedAt = r.CreatedAt
        };
    }

    public async Task AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Hangfire job: AutoLockExpiredTimesheetsAsync");

        try
        {
            var lockedResults = await _timesheetRepo.AutoLockExpiredTimesheetsAsync(cancellationToken);

            foreach (var item in lockedResults)
            {
                _logger.LogInformation(
                    "Auto-locked timesheets for week {ScheduleWeekId} ({Count} timesheets locked)",
                    item.Week.ScheduleWeekId,
                    item.LockedTimesheets.Count);

                if (item.NotificationId > 0 && item.StaffUserIds.Any())
                {
                    foreach (var staffId in item.StaffUserIds)
                    {
                        try
                        {
                            await _realtimePublisher.PublishAsync(staffId, new RealtimeNotificationResponse
                            {
                                NotificationId = item.NotificationId,
                                Title = "Timesheet Finalized and Locked",
                                Message = item.NotificationContent,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to publish realtime lock notification to staff {StaffId}", staffId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Hangfire job AutoLockExpiredTimesheetsAsync");
            throw;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportWeeklyTimesheetExcelAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        var detail = await GetWeeklyTimesheetDetailAsync(managerId, scheduleWeekId, cancellationToken)
            ?? throw new KeyNotFoundException($"Weekly timesheet details not found for week ID {scheduleWeekId}.");

        if (string.Equals(detail.Status, "NOT GENERATED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot export timesheet: Timesheet has not been generated for this week yet. Please generate the timesheet first.");
        }

        string exportedByName = string.IsNullOrWhiteSpace(manager.FullName) ? "Branch Manager" : manager.FullName;
        return TimesheetExcelExporter.GenerateWeeklyTimesheetExcel(detail, exportedByName);
    }
}

