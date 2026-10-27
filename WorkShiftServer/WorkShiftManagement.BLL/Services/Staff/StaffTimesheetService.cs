using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Staff.Timesheet;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public sealed class StaffTimesheetService : IStaffTimesheetService
{
    private readonly IStaffTimesheetRepo _repo;
    private readonly ILogger<StaffTimesheetService> _logger;

    public StaffTimesheetService(
        IStaffTimesheetRepo repo,
        ILogger<StaffTimesheetService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<StaffWeeklyTimesheetResponse?> GetMyWeeklyTimesheetAsync(
        Guid staffId,
        int? scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        if (staffId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated staff identity is required.");
        }

        var staff = await _repo.GetActiveStaffAsync(staffId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Staff account is inactive or was not found.");

        int branchId = staff.BranchId
            ?? throw new InvalidOperationException("Staff is not currently assigned to any branch.");

        var availableWeeks = await _repo.GetAvailableWeeksForStaffAsync(branchId, cancellationToken);
        if (availableWeeks.Count == 0)
        {
            return null;
        }

        int targetWeekId;
        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            targetWeekId = scheduleWeekId.Value;
        }
        else
        {
            // Pick most recent week with generated timesheet for this staff, or fallback to latest week
            var weekWithTimesheet = availableWeeks.FirstOrDefault(w =>
                w.PayrollPeriod != null &&
                w.PayrollPeriod.Timesheets.Any(t => t.UserId == staffId));

            targetWeekId = weekWithTimesheet?.ScheduleWeekId ?? availableWeeks[0].ScheduleWeekId;
        }

        var week = await _repo.GetWeeklyScheduleWithStaffShiftsAsync(targetWeekId, branchId, cancellationToken);
        if (week == null)
        {
            return null;
        }

        var staffTimesheet = week.PayrollPeriod?.Timesheets.FirstOrDefault(t => t.UserId == staffId);

        var staffAssignments = (week.WorkShifts ?? Enumerable.Empty<WorkShift>())
            .Where(s => !string.Equals(s.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .SelectMany(s => (s.ShiftAssignments ?? Enumerable.Empty<ShiftAssignment>())
                .Where(a => a.UserId == staffId && !string.Equals(a.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .Select(a => new { Shift = s, Assignment = a }))
            .OrderBy(x => x.Shift.ScheduledStartAt)
            .ToList();

        string status;
        string staffReviewStatus;
        int? timesheetId;
        int totalAssigned;
        int completedShifts;
        int absentShifts;
        int incompleteShifts;
        int totalWorkMinutes;
        int totalLateMinutes;
        int totalEarlyLeaveMinutes;
        bool canConfirm;
        bool isLocked;
        DateTime? reviewDeadline = null;
        bool isPastDeadline = false;
        int pendingAdjustmentCount = 0;

        if (staffTimesheet != null)
        {
            status = staffTimesheet.Status ?? "GENERATED";
            staffReviewStatus = staffTimesheet.StaffReviewStatus ?? "PENDING";
            timesheetId = staffTimesheet.TimesheetId;
            totalAssigned = staffTimesheet.TotalAssignedShifts;
            completedShifts = staffTimesheet.CompletedShiftCount;
            absentShifts = staffTimesheet.AbsentShiftCount;
            incompleteShifts = staffTimesheet.IncompleteShiftCount;
            totalWorkMinutes = staffTimesheet.TotalWorkMinutes;
            totalLateMinutes = staffTimesheet.TotalLateMinutes;
            totalEarlyLeaveMinutes = staffTimesheet.TotalEarlyLeaveMinutes;
            isLocked = string.Equals(status, "LOCKED", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "REVIEWED", StringComparison.OrdinalIgnoreCase);

            reviewDeadline = staffTimesheet.ReviewDeadline;
            if (!reviewDeadline.HasValue)
            {
                var firstGeneratedAt = staffTimesheet.GeneratedAt ?? week.PayrollPeriod?.CreatedAt ?? DateTime.UtcNow;
                var vnOffset = TimeSpan.FromHours(7);
                var generatedAtVn = firstGeneratedAt + vnOffset;
                var endOfDayVn = generatedAtVn.Date.AddDays(1).AddTicks(-1);
                reviewDeadline = endOfDayVn - vnOffset;
            }
            isPastDeadline = DateTime.UtcNow >= reviewDeadline.Value;

            pendingAdjustmentCount = staffTimesheet.TimesheetAdjustmentRequests?
                .Count(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase)) ?? 0;

            canConfirm = string.Equals(status, "GENERATED", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(staffReviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                         pendingAdjustmentCount == 0 &&
                         !isPastDeadline;
        }
        else
        {
            status = "NOT GENERATED";
            staffReviewStatus = "-";
            timesheetId = null;
            totalAssigned = staffAssignments.Count;
            completedShifts = 0;
            absentShifts = 0;
            incompleteShifts = 0;
            totalWorkMinutes = 0;
            totalLateMinutes = 0;
            totalEarlyLeaveMinutes = 0;
            canConfirm = false;
            isLocked = false;
        }

        var allAdjsByAttendance = staffTimesheet?.TimesheetAdjustmentRequests?
            .GroupBy(r => r.AttendanceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ToList())
            ?? new Dictionary<int, List<TimesheetAdjustmentRequest>>();

        var shiftDtos = staffAssignments.Select(x =>
        {
            var att = x.Assignment.Attendance;
            string attStatus = att?.Status
                ?? (DateTime.UtcNow > x.Shift.ScheduledEndAt ? "ABSENT" : "SCHEDULED");

            var reqList = att != null && allAdjsByAttendance.TryGetValue(att.AttendanceId, out var foundList)
                ? foundList
                : new List<TimesheetAdjustmentRequest>();

            var adj = reqList.FirstOrDefault();

            bool hasAdj = adj != null;
            string? adjStatus = adj?.Status;

            bool hasPending = reqList.Any(r => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

            var checkInReq = reqList.FirstOrDefault(r => r.AdjustCheckIn && !string.Equals(r.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase));
            bool isCheckInLocked = checkInReq != null;
            string? checkInLockReason = checkInReq?.Status;

            var checkOutReq = reqList.FirstOrDefault(r => r.AdjustCheckOut && !string.Equals(r.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase));
            bool isCheckOutLocked = checkOutReq != null;
            string? checkOutLockReason = checkOutReq?.Status;

            bool canRequestAdj = !isLocked &&
                                 string.Equals(status, "GENERATED", StringComparison.OrdinalIgnoreCase) &&
                                 !isPastDeadline &&
                                 !string.Equals(staffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase) &&
                                 !hasPending &&
                                 (!isCheckInLocked || !isCheckOutLocked);

            bool canCancelAdj = hasPending && !isLocked && !isPastDeadline;

            var approvedLeave = x.Assignment.LeaveRequestAssignments
                ?.Select(l => l.Request)
                ?.FirstOrDefault(r => r != null && string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

            bool isExcusedLeave = approvedLeave != null || string.Equals(attStatus, "LEAVE", StringComparison.OrdinalIgnoreCase);
            string? leaveReason = approvedLeave?.Reason;

            return new StaffShiftAttendanceDto
            {
                AssignmentId = x.Assignment.AssignmentId,
                AttendanceId = att?.AttendanceId,
                ShiftId = x.Shift.ShiftId,
                ShiftName = x.Shift.ShiftNameSnapshot,
                ScheduledStartAt = x.Shift.ScheduledStartAt,
                ScheduledEndAt = x.Shift.ScheduledEndAt,
                CheckInTime = att?.CheckInTime,
                CheckOutTime = att?.CheckOutTime,
                ActualWorkingMinutes = att?.ActualWorkingMinutes ?? 0,
                TotalLateMinutes = att?.TotalLateMinutes ?? 0,
                ExcusedLateMinutes = att?.ExcusedLateMinutes ?? 0,
                ChargeableLateMinutes = att?.ChargeableLateMinutes ?? 0,
                TotalEarlyLeaveMinutes = att?.TotalEarlyLeaveMinutes ?? 0,
                ExcusedEarlyLeaveMinutes = att?.ExcusedEarlyLeaveMinutes ?? 0,
                ChargeableEarlyLeaveMinutes = att?.ChargeableEarlyLeaveMinutes ?? 0,
                HasApprovedLateArrival = att?.LateArrivalRequestId.HasValue ?? false,
                HasApprovedEarlyLeave = att?.EarlyLeaveRequestId.HasValue ?? false,
                AttendanceStatus = isExcusedLeave ? "LEAVE" : attStatus,
                AssignmentStatus = x.Assignment.Status,
                IsExcusedLeave = isExcusedLeave,
                LeaveReason = leaveReason,

                HasAdjustmentRequest = hasAdj,
                AdjustmentRequestId = adj?.AdjustmentRequestId,
                AdjustmentStatus = adjStatus,
                RequestedCheckInTime = adj?.RequestedCheckInTime,
                RequestedCheckOutTime = adj?.RequestedCheckOutTime,
                AdjustmentReason = adj?.Reason,
                ManagerNote = adj?.ManagerNote,
                CanRequestAdjustment = canRequestAdj,
                CanCancelAdjustment = canCancelAdj,
                IsCheckInLocked = isCheckInLocked,
                CheckInLockReason = checkInLockReason,
                IsCheckOutLocked = isCheckOutLocked,
                CheckOutLockReason = checkOutLockReason,
                Penalties = att?.AttendancePenaltyRecords
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new StaffShiftAttendancePenaltyDto
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

        if (staffTimesheet == null)
        {
            completedShifts = shiftDtos.Count(s => s.AttendanceStatus is "COMPLETED" or "PRESENT" or "LATE" or "EARLY_LEAVE");
            absentShifts = shiftDtos.Count(s => s.AttendanceStatus == "ABSENT");
            incompleteShifts = shiftDtos.Count(s => s.AttendanceStatus == "INCOMPLETE");
            totalWorkMinutes = shiftDtos.Sum(s => s.ActualWorkingMinutes);
            totalLateMinutes = shiftDtos.Sum(s => s.TotalLateMinutes);
            totalEarlyLeaveMinutes = shiftDtos.Sum(s => s.TotalEarlyLeaveMinutes);
        }

        decimal totalWorkHours = Math.Round((decimal)totalWorkMinutes / 60m, 2);

        var weekOptions = availableWeeks.Select(w =>
        {
            var t = w.PayrollPeriod?.Timesheets.FirstOrDefault(ts => ts.UserId == staffId);
            return new StaffTimesheetWeekOptionDto
            {
                ScheduleWeekId = w.ScheduleWeekId,
                Period = $"{w.WeekStartDate:dd/MM} - {w.WeekEndDate:dd/MM}",
                WeekStartDate = w.WeekStartDate,
                WeekEndDate = w.WeekEndDate,
                HasTimesheet = t != null,
                Status = t != null ? (t.Status ?? "GENERATED") : "NOT GENERATED"
            };
        }).ToList();

        var activeRule = await _repo.GetActiveAttendanceRuleAsync(cancellationToken);

        return new StaffWeeklyTimesheetResponse
        {
            ScheduleWeekId = week.ScheduleWeekId,
            PayrollPeriodId = week.PayrollPeriod?.PayrollPeriodId,
            TimesheetId = timesheetId,
            Period = $"{week.WeekStartDate:dd/MM} - {week.WeekEndDate:dd/MM}",
            WeekStartDate = week.WeekStartDate,
            WeekEndDate = week.WeekEndDate,
            BranchId = week.BranchId,
            BranchName = week.Branch?.BranchName ?? $"Branch {week.BranchId}",
            Status = status,
            StaffReviewStatus = staffReviewStatus,
            CanConfirm = canConfirm,
            IsLocked = isLocked,
            ReviewDeadline = reviewDeadline,
            IsPastDeadline = isPastDeadline,
            PendingAdjustmentCount = pendingAdjustmentCount,
            CheckInBeforeMinutes = activeRule?.CheckInBeforeMinutes ?? 30,
            CheckOutAfterMinutes = activeRule?.CheckOutAfterMinutes ?? 60,
            TotalAssignedShifts = totalAssigned,
            CompletedShifts = completedShifts,
            AbsentShifts = absentShifts,
            IncompleteShifts = incompleteShifts,
            TotalWorkHours = totalWorkHours,
            TotalWorkMinutes = totalWorkMinutes,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
            Shifts = shiftDtos,
            AvailableWeeks = weekOptions
        };
    }

    public async Task<StaffWeeklyTimesheetResponse> ConfirmTimesheetAsync(
        Guid staffId,
        int scheduleWeekId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (staffId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated staff identity is required.");
        }

        if (scheduleWeekId <= 0)
        {
            throw new ArgumentException("Valid schedule week ID is required.");
        }

        var staff = await _repo.GetActiveStaffAsync(staffId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Staff account is inactive or was not found.");

        var timesheet = await _repo.GetStaffTimesheetForWeekAsync(scheduleWeekId, staffId, cancellationToken);
        if (timesheet == null)
        {
            throw new KeyNotFoundException("Timesheet record for this week was not found or has not been generated yet.");
        }

        await _repo.ConfirmTimesheetAsync(timesheet.TimesheetId, staffId, note, cancellationToken);

        var updated = await GetMyWeeklyTimesheetAsync(staffId, scheduleWeekId, cancellationToken);
        return updated ?? throw new InvalidOperationException("Failed to retrieve timesheet after confirmation.");
    }

    public async Task<StaffWeeklyTimesheetResponse> SubmitAdjustmentRequestsAsync(
        Guid staffId,
        SubmitTimesheetAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (staffId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated staff identity is required.");
        }

        if (request == null || request.ScheduleWeekId <= 0)
        {
            throw new ArgumentException("Valid schedule week ID is required.");
        }

        if (request.Adjustments == null || !request.Adjustments.Any())
        {
            throw new ArgumentException("Adjustments list cannot be empty.");
        }

        var staff = await _repo.GetActiveStaffAsync(staffId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Staff account is inactive or was not found.");

        var submissionItems = request.Adjustments.Select(a => new StaffAdjustmentSubmissionItem
        {
            AssignmentId = a.AssignmentId,
            AttendanceId = a.AttendanceId,
            AdjustCheckIn = a.AdjustCheckIn,
            RequestedCheckInTime = a.RequestedCheckInTime,
            AdjustCheckOut = a.AdjustCheckOut,
            RequestedCheckOutTime = a.RequestedCheckOutTime,
            Reason = a.Reason
        }).ToList();

        await _repo.SubmitAdjustmentRequestsAsync(request.ScheduleWeekId, staffId, submissionItems, cancellationToken);

        var updated = await GetMyWeeklyTimesheetAsync(staffId, request.ScheduleWeekId, cancellationToken);
        return updated ?? throw new InvalidOperationException("Failed to retrieve updated timesheet after submitting adjustments.");
    }

    public async Task<StaffWeeklyTimesheetResponse> CancelAdjustmentRequestAsync(
        Guid staffId,
        int adjustmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (staffId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated staff identity is required.");
        }

        if (adjustmentRequestId <= 0)
        {
            throw new ArgumentException("Valid adjustment request ID is required.");
        }

        var (_, scheduleWeekId) =
            await _repo.CancelAdjustmentRequestAsync(adjustmentRequestId, staffId, cancellationToken);

        var updated = await GetMyWeeklyTimesheetAsync(staffId, scheduleWeekId, cancellationToken);
        return updated ?? throw new InvalidOperationException("Failed to retrieve updated timesheet after cancelling adjustment.");
    }
}
