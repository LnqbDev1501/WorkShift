using WorkShiftManagement.BLL.Interfaces.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Common;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ManagerTimesheetService : IManagerTimesheetService
{
    private readonly IAdminRealtimePublisher? _adminRealtimePublisher;
    private readonly IManagerTimesheetRepo _timesheetRepo;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly ILogger<ManagerTimesheetService> _logger;
    private readonly IAuditLogHelper? _auditLogHelper;

    public ManagerTimesheetService(
        IManagerTimesheetRepo timesheetRepo,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<ManagerTimesheetService> logger,
        IAdminRealtimePublisher? adminRealtimePublisher = null,
        IAuditLogHelper? auditLogHelper = null)
    {
        _timesheetRepo = timesheetRepo;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
        _adminRealtimePublisher = adminRealtimePublisher;
        _auditLogHelper = auditLogHelper;
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

    private static (int TotalStaff, int ConfirmedStaff, int NoResponseStaff, int TotalWorkMinutes, int TotalLateMinutes, int TotalEarlyLeaveMinutes, string Status)
        ComputeSummaryMetrics(WorkScheduleWeek week, List<Timesheet> timesheets)
    {
        if (timesheets.Count > 0)
        {
            int totalStaff = timesheets.Count;
            int confirmedStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase));
            int noResponseStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase));
            int totalWorkMinutes = timesheets.Sum(t => t.TotalWorkMinutes);
            int totalLateMinutes = timesheets.Sum(t => t.TotalLateMinutes);
            int totalEarlyLeaveMinutes = timesheets.Sum(t => t.TotalEarlyLeaveMinutes);

            string status = timesheets.All(t => string.Equals(t.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase))
                ? "REVIEWED"
                : (timesheets.All(t => string.Equals(t.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
                    ? "LOCKED"
                    : (!string.IsNullOrWhiteSpace(week.PayrollPeriod?.Status) ? week.PayrollPeriod.Status : "GENERATED"));

            return (totalStaff, confirmedStaff, noResponseStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status);
        }

        int ungeneratedStaff = (week.WorkShifts ?? Enumerable.Empty<WorkShift>())
            .SelectMany(s => s.ShiftAssignments ?? Enumerable.Empty<ShiftAssignment>())
            .Where(a => a.Status != "CANCELLED")
            .Select(a => a.UserId)
            .Distinct()
            .Count();

        return (ungeneratedStaff, 0, 0, 0, 0, 0, "NOT GENERATED");
    }

    private static ManagerWeeklyTimesheetSummaryResponse MapWeeklySummary(WorkScheduleWeek week)
    {
        var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
        var (totalStaff, confirmedStaff, noResponseStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status) =
            ComputeSummaryMetrics(week, timesheets);

        string staffReview = status == "NOT GENERATED"
            ? "-"
            : (noResponseStaff > 0
                ? $"{confirmedStaff}/{totalStaff} Confirmed ({noResponseStaff} No Response)"
                : $"{confirmedStaff}/{totalStaff} Confirmed");

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
            NoResponseStaff = noResponseStaff,
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

        var activeRule = await _timesheetRepo.GetActiveAttendanceRuleAsync(cancellationToken);
        int defaultCheckOutAfterMinutes = activeRule?.CheckOutAfterMinutes ?? 60;
        var nowUtc = DateTime.UtcNow;

        var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
        var (totalStaff, confirmedStaff, noResponseStaff, totalWorkMinutes, totalLateMinutes, totalEarlyLeaveMinutes, status) =
            ComputeSummaryMetrics(week, timesheets);

        var staffItems = new List<ManagerStaffTimesheetItemDto>();

        var assignmentsByUser = (week.WorkShifts ?? Enumerable.Empty<WorkShift>())
            .Where(s => !string.Equals(s.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .SelectMany(s => (s.ShiftAssignments ?? Enumerable.Empty<ShiftAssignment>()).Select(a => new { Shift = s, Assignment = a }))
            .Where(x => !string.Equals(x.Assignment.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Assignment.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Shift.ScheduledStartAt).ToList());

        List<ManagerStaffShiftAttendanceDto> MapUserShifts(Guid userId)
        {
            if (!assignmentsByUser.TryGetValue(userId, out var userItems))
                return new List<ManagerStaffShiftAttendanceDto>();

            return userItems.Select(x =>
            {
                var att = x.Assignment.Attendance;
                var approvedLeave = x.Assignment.LeaveRequestAssignments
                    ?.Select(l => l.Request)
                    ?.FirstOrDefault(r => r != null && string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

                bool isExcusedLeave = approvedLeave != null || string.Equals(att?.Status, "LEAVE", StringComparison.OrdinalIgnoreCase);
                string? leaveReason = approvedLeave?.Reason;

                string attStatus;
                if (isExcusedLeave)
                {
                    attStatus = "LEAVE";
                }
                else if (att != null)
                {
                    if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                    {
                        attStatus = string.Equals(att.Status, "INCOMPLETE", StringComparison.OrdinalIgnoreCase)
                            ? "ABSENT"
                            : (att.Status ?? "COMPLETED");
                    }
                    else if (string.Equals(att.Status, "ABSENT", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(att.Status, "INCOMPLETE", StringComparison.OrdinalIgnoreCase))
                    {
                        attStatus = "ABSENT";
                    }
                    else if (att.CheckInTime.HasValue && !att.CheckOutTime.HasValue)
                    {
                        var checkOutCutoffUtc = x.Shift.ScheduledEndAt.AddMinutes(GetCheckOutAfterMinutes(att, defaultCheckOutAfterMinutes));
                        attStatus = nowUtc > checkOutCutoffUtc ? "ABSENT" : (att.Status ?? "IN_PROGRESS");
                    }
                    else
                    {
                        attStatus = nowUtc > x.Shift.ScheduledEndAt ? "ABSENT" : (att.Status ?? "SCHEDULED");
                    }
                }
                else
                {
                    attStatus = nowUtc > x.Shift.ScheduledEndAt ? "ABSENT" : "SCHEDULED";
                }

                bool isAbsentStatus = !isExcusedLeave && string.Equals(attStatus, "ABSENT", StringComparison.OrdinalIgnoreCase);

                return new ManagerStaffShiftAttendanceDto
                {
                    AssignmentId = x.Assignment.AssignmentId,
                    ShiftId = x.Shift.ShiftId,
                    ShiftName = x.Shift.ShiftNameSnapshot,
                    ScheduledStartAt = x.Shift.ScheduledStartAt,
                    ScheduledEndAt = x.Shift.ScheduledEndAt,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    ActualWorkingMinutes = isAbsentStatus ? 0 : (att?.ActualWorkingMinutes ?? 0),
                    TotalLateMinutes = isAbsentStatus ? 0 : (att?.TotalLateMinutes ?? 0),
                    ExcusedLateMinutes = isAbsentStatus ? 0 : (att?.ExcusedLateMinutes ?? 0),
                    ChargeableLateMinutes = isAbsentStatus ? 0 : (att?.ChargeableLateMinutes ?? 0),
                    TotalEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.TotalEarlyLeaveMinutes ?? 0),
                    ExcusedEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.ExcusedEarlyLeaveMinutes ?? 0),
                    ChargeableEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.ChargeableEarlyLeaveMinutes ?? 0),
                    HasApprovedLateArrival = att?.LateArrivalRequestId.HasValue ?? false,
                    HasApprovedEarlyLeave = att?.EarlyLeaveRequestId.HasValue ?? false,
                    AttendanceStatus = attStatus,
                    AssignmentStatus = x.Assignment.Status,
                    IsExcusedLeave = isExcusedLeave,
                    LeaveReason = leaveReason,
                    Penalties = att?.AttendancePenaltyRecords?
                        .OrderBy(p => p.CreatedAt)
                        .Select(p => new WorkShiftManagement.BLL.DTOs.Manager.Attendance.AttendancePenaltyDto
                        {
                            AttendancePenaltyId = p.AttendancePenaltyId,
                            ViolationType = p.ViolationType,
                            ViolationMinutes = p.ViolationMinutes,
                            PenaltyAmount = p.PenaltyAmount,
                            Status = p.Status,
                            CreatedAt = p.CreatedAt
                        }).ToList() ?? new()
                };
            }).ToList();
        }

        if (timesheets.Count > 0)
        {
            foreach (var t in timesheets.OrderBy(t => t.User.FullName))
            {
                var userShifts = MapUserShifts(t.UserId);
                int lateCount = userShifts.Count(s => s.AttendanceStatus != "ABSENT" && s.TotalLateMinutes > 0);
                int earlyLeaveCount = userShifts.Count(s => s.AttendanceStatus != "ABSENT" && s.TotalEarlyLeaveMinutes > 0);
                int excusedLeaveCount = userShifts.Count(s => s.IsExcusedLeave);
                int unexcusedAbsentCount = userShifts.Count(s => !s.IsExcusedLeave && s.AttendanceStatus == "ABSENT");
                int incompleteCount = 0;

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
                    IncompleteCount = incompleteCount,
                    StaffReviewStatus = t.StaffReviewStatus ?? "PENDING",
                    Status = t.Status ?? status,
                    ReviewDeadline = t.ReviewDeadline,
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
                int lateCount = userShifts.Count(s => s.AttendanceStatus != "ABSENT" && s.TotalLateMinutes > 0);
                int earlyLeaveCount = userShifts.Count(s => s.AttendanceStatus != "ABSENT" && s.TotalEarlyLeaveMinutes > 0);
                int excusedLeaveCount = userShifts.Count(s => s.IsExcusedLeave);
                int unexcusedAbsentCount = userShifts.Count(s => !s.IsExcusedLeave && s.AttendanceStatus == "ABSENT");
                int incompleteCount = 0;

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
                    IncompleteCount = incompleteCount,
                    StaffReviewStatus = "-",
                    Status = "NOT GENERATED",
                    Shifts = userShifts
                });
            }
        }

        string staffReview = status == "NOT GENERATED"
            ? "-"
            : (noResponseStaff > 0
                ? $"{confirmedStaff}/{totalStaff} Confirmed ({noResponseStaff} No Response)"
                : $"{confirmedStaff}/{totalStaff} Confirmed");

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
            var vnOffset = TimeSpan.FromHours(7);
            var now = DateTime.UtcNow;

            var pendingStaffList = timesheets
                .Where(t => string.Equals(t.StaffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                .ToList();

            reviewDeadline = timesheets
                .Where(t => t.ReviewDeadline.HasValue)
                .Select(t => t.ReviewDeadline)
                .DefaultIfEmpty()
                .Max();

            if (!reviewDeadline.HasValue)
            {
                var latestGeneratedAt = timesheets.Where(t => t.GeneratedAt.HasValue).Max(t => t.GeneratedAt)
                    ?? week.PayrollPeriod?.CreatedAt
                    ?? now;
                var generatedAtVn = latestGeneratedAt + vnOffset;
                var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
                reviewDeadline = endOfDayVn - vnOffset;
            }

            bool hasActivePendingReview = pendingStaffList.Any(t =>
            {
                var tDeadlineUtc = t.ReviewDeadline ?? (t.GeneratedAt ?? week.PayrollPeriod?.CreatedAt ?? now).AddHours(24);
                return now < tDeadlineUtc;
            });

            isPastDeadline = !hasActivePendingReview && (now >= reviewDeadline.Value);
            bool isAllConfirmed = (totalStaff > 0 && confirmedStaff == totalStaff);
            bool isAllResolved = (totalStaff > 0 && (confirmedStaff + noResponseStaff == totalStaff));

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
            else if (isAllResolved || !hasActivePendingReview)
            {
                canLockTimesheet = true;
                lockEligibilityMessage = noResponseStaff > 0
                    ? $"Ready to review: {confirmedStaff}/{totalStaff} confirmed, {noResponseStaff} no response (review deadline expired)."
                    : "Ready to review: Review deadline has expired at end of day.";
            }
            else
            {
                canLockTimesheet = false;
                var deadlineVn = reviewDeadline.Value + vnOffset;
                lockEligibilityMessage = $"Waiting for staff confirmation ({confirmedStaff}/{totalStaff} confirmed) or wait until review deadline expires ({deadlineVn:HH:mm dd/MM/yyyy}).";
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

        string? reviewedByName = timesheets
            .FirstOrDefault(t => t.ReviewedByNavigation != null)?.ReviewedByNavigation?.FullName;

        if (string.IsNullOrWhiteSpace(reviewedByName))
        {
            var reviewerId = timesheets.FirstOrDefault(t => t.ReviewedBy.HasValue)?.ReviewedBy;
            if (reviewerId.HasValue)
            {
                reviewedByName = await _timesheetRepo.GetUserNameByIdAsync(reviewerId.Value, cancellationToken);
            }
        }

        string? lockedByName = timesheets
            .FirstOrDefault(t => string.Equals(t.LockedByNavigation?.Role?.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase))?.LockedByNavigation?.FullName
            ?? (string.Equals(week.PayrollPeriod?.ApprovedByNavigation?.Role?.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase)
                ? week.PayrollPeriod?.ApprovedByNavigation?.FullName
                : null);

        if (string.IsNullOrWhiteSpace(lockedByName))
        {
            var lockerId = timesheets.FirstOrDefault(t => t.LockedBy.HasValue)?.LockedBy
                ?? week.PayrollPeriod?.ApprovedBy;
            if (lockerId.HasValue)
            {
                var userName = await _timesheetRepo.GetUserNameByIdAsync(lockerId.Value, cancellationToken);
                if (!string.IsNullOrWhiteSpace(userName) && !string.Equals(userName, reviewedByName, StringComparison.OrdinalIgnoreCase))
                {
                    lockedByName = userName;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(lockedByName) && string.Equals(status, "LOCKED", StringComparison.OrdinalIgnoreCase))
        {
            lockedByName = await _timesheetRepo.GetDefaultDirectorNameAsync(cancellationToken);
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
            NoResponseStaff = noResponseStaff,
            StaffReview = staffReview,
            ConfirmationRate = confirmationRate,
            TotalWorkHours = totalWorkHours,
            TotalWorkMinutes = totalWorkMinutes,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
            ReviewedByName = reviewedByName,
            LockedByName = lockedByName,
            StaffTimesheets = staffItems
        };
    }

    public async Task<ManagerWeeklyTimesheetDetailResponse> GenerateWeeklyTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        GenerateWeeklyTimesheetRequestDto? request = null,
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

        var (week, generatedTimesheets, staffUserIds, notificationId, notificationContent) =
            await _timesheetRepo.GenerateWeeklyTimesheetAsync(scheduleWeekId, branchId, managerId, request?.ReviewDeadline, cancellationToken);

        if (_auditLogHelper != null)
        {
            var periodId = week.PayrollPeriod?.PayrollPeriodId;
            await _auditLogHelper.LogAsync(
                action: "GENERATE_BRANCH_TIMESHEET",
                tableName: periodId.HasValue ? "payroll_periods" : "timesheets",
                recordId: (periodId ?? scheduleWeekId).ToString(),
                oldValue: null,
                newValue: new
                {
                    week.ScheduleWeekId,
                    PayrollPeriodId = periodId,
                    week.BranchId,
                    TimesheetCount = generatedTimesheets.Count,
                    StaffCount = staffUserIds.Count,
                    ApprovalDeadlineAt = week.PayrollPeriod?.ApprovalDeadlineAt
                },
                actorUserId: managerId,
                cancellationToken: cancellationToken);
        }

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

        var (week, lockedTimesheets, staffUserIds, accountantUserIds, notificationId, accountantNotificationId, notificationContent, accountantNotificationContent) =
            await _timesheetRepo.LockWeeklyTimesheetAsync(scheduleWeekId, branchId, managerId, cancellationToken);

        if (_auditLogHelper != null)
        {
            var periodId = week.PayrollPeriod?.PayrollPeriodId;
            await _auditLogHelper.LogAsync(
                action: "REVIEW_BRANCH_TIMESHEET",
                tableName: periodId.HasValue ? "payroll_periods" : "timesheets",
                recordId: (periodId ?? scheduleWeekId).ToString(),
                oldValue: new { Status = "GENERATED" },
                newValue: new
                {
                    week.ScheduleWeekId,
                    PayrollPeriodId = periodId,
                    week.BranchId,
                    Status = "REVIEWED",
                    ReviewedCount = lockedTimesheets.Count
                },
                actorUserId: managerId,
                cancellationToken: cancellationToken);
        }

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

    public async Task<ManagerWeeklyTimesheetDetailResponse> ReopenStaffTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        int timesheetId,
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

        if (timesheetId <= 0)
        {
            throw new ArgumentException("Timesheet ID must be greater than zero.");
        }

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var (_, resolvedScheduleWeekId, staffUserId, notificationId, notificationContent) =
            await _timesheetRepo.ReopenStaffTimesheetAsync(timesheetId, branchId, managerId, cancellationToken);

        if (notificationId > 0 && staffUserId != Guid.Empty)
        {
            try
            {
                await _realtimePublisher.PublishAsync(staffUserId, new RealtimeNotificationResponse
                {
                    NotificationId = notificationId,
                    Title = "Weekly Timesheet Review Window Reopened",
                    Message = notificationContent,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish realtime notification to staff member {StaffUserId}", staffUserId);
            }
        }

        var detail = await GetWeeklyTimesheetDetailAsync(managerId, resolvedScheduleWeekId, cancellationToken);
        return detail ?? throw new InvalidOperationException("Timesheet details could not be retrieved after reopening.");
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

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "APPROVE_TIMESHEET_ADJUSTMENT",
                tableName: "timesheet_adjustment_requests",
                recordId: approved.AdjustmentRequestId.ToString(),
                oldValue: new
                {
                    Status = "PENDING",
                    OldCheckInTime = approved.CurrentCheckInSnapshot,
                    OldCheckOutTime = approved.CurrentCheckOutSnapshot
                },
                newValue: new
                {
                    approved.AdjustmentRequestId,
                    approved.TimesheetId,
                    approved.AttendanceId,
                    StaffId = approved.UserId,
                    approved.Status,
                    approved.AdjustCheckIn,
                    approved.RequestedCheckInTime,
                    approved.AdjustCheckOut,
                    approved.RequestedCheckOutTime,
                    approved.ReviewedBy,
                    approved.ReviewedAt
                },
                actorUserId: managerId,
                cancellationToken: cancellationToken);
        }

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

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "REJECT_TIMESHEET_ADJUSTMENT",
                tableName: "timesheet_adjustment_requests",
                recordId: rejected.AdjustmentRequestId.ToString(),
                oldValue: new { Status = "PENDING" },
                newValue: new
                {
                    rejected.AdjustmentRequestId,
                    rejected.TimesheetId,
                    rejected.AttendanceId,
                    StaffId = rejected.UserId,
                    rejected.Status,
                    rejected.ManagerNote,
                    rejected.ReviewedBy,
                    rejected.ReviewedAt
                },
                actorUserId: managerId,
                cancellationToken: cancellationToken);
        }

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
            CreatedAt = r.CreatedAt,
            AuditHistories = r.Attendance?.AttendanceAdjustmentHistories?
                .OrderByDescending(h => h.AdjustedAt)
                .Select(h => new AttendanceAdjustmentAuditHistoryDto
                {
                    AdjustmentHistoryId = h.AdjustmentHistoryId,
                    AttendanceId = h.AttendanceId,
                    AdjustedBy = h.AdjustedBy,
                    AdjustedByName = h.AdjustedByNavigation?.FullName,
                    AdjustmentSource = h.AdjustmentSource,
                    Reason = h.Reason,
                    OldCheckInTime = h.OldCheckInTime,
                    NewCheckInTime = h.NewCheckInTime,
                    OldCheckOutTime = h.OldCheckOutTime,
                    NewCheckOutTime = h.NewCheckOutTime,
                    OldActualWorkingMinutes = h.OldActualWorkingMinutes,
                    NewActualWorkingMinutes = h.NewActualWorkingMinutes,
                    OldTotalLateMinutes = h.OldTotalLateMinutes,
                    NewTotalLateMinutes = h.NewTotalLateMinutes,
                    OldChargeableLateMinutes = h.OldChargeableLateMinutes,
                    NewChargeableLateMinutes = h.NewChargeableLateMinutes,
                    OldTotalEarlyLeaveMinutes = h.OldTotalEarlyLeaveMinutes,
                    NewTotalEarlyLeaveMinutes = h.NewTotalEarlyLeaveMinutes,
                    OldChargeableEarlyLeaveMinutes = h.OldChargeableEarlyLeaveMinutes,
                    NewChargeableEarlyLeaveMinutes = h.NewChargeableEarlyLeaveMinutes,
                    AdjustedAt = h.AdjustedAt
                }).ToList() ?? new()
        };
    }

    public async Task AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Hangfire job: AutoLockExpiredTimesheetsAsync");

        try
        {
            var lockedResults = await _timesheetRepo.AutoLockExpiredTimesheetsAsync(cancellationToken);
            if (lockedResults.Any(item => item.LockedTimesheets.Count > 0) && _adminRealtimePublisher != null)
                await _adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                    new { resource = "Timesheets", action = "AUTO_LOCKED" });

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

    private static int GetCheckOutAfterMinutes(Attendance? att, int fallbackMinutes)
    {
        if (att != null && !string.IsNullOrWhiteSpace(att.RuleSnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<AttendanceRuleSnapshotRoot>(
                    att.RuleSnapshotJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (snapshot?.AttendanceRule != null && snapshot.AttendanceRule.CheckOutAfterMinutes > 0)
                {
                    return snapshot.AttendanceRule.CheckOutAfterMinutes;
                }
            }
            catch
            {
                // Ignore malformed snapshot and fallback to active rule
            }
        }
        return fallbackMinutes;
    }
}

