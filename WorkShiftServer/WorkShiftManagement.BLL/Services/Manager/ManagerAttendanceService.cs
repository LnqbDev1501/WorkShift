using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Manager.Attendance;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerAttendanceService : IManagerAttendanceService
{
    private readonly IManagerAttendanceRepo _attendanceRepo;

    public ManagerAttendanceService(IManagerAttendanceRepo attendanceRepo)
    {
        _attendanceRepo = attendanceRepo;
    }

    public async Task<ManagerDailyAttendanceResponse> GetDailyAttendanceAsync(
        Guid managerId,
        ManagerDailyAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        ArgumentNullException.ThrowIfNull(query);

        var manager = await _attendanceRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        // 1. Fetch available weeks for branch
        var weeks = await _attendanceRepo.GetAvailableWeeksForBranchAsync(branchId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var availableWeeks = weeks.Select(w => new DailyAttendanceWeekLookupDto
        {
            ScheduleWeekId = w.ScheduleWeekId,
            WeekStartDate = w.WeekStartDate,
            WeekEndDate = w.WeekEndDate,
            PeriodName = $"Week #{w.ScheduleWeekId} ({w.WeekStartDate:dd/MM} – {w.WeekEndDate:dd/MM})",
            Status = w.Status,
            IsCurrentWeek = w.WeekStartDate <= today && today <= w.WeekEndDate
        }).ToList();

        // 2. Fetch available shifts for filter
        var shifts = await _attendanceRepo.GetShiftsForBranchAsync(branchId, query.ScheduleWeekId, query.Date, cancellationToken);
        var availableShifts = shifts.Select(s => new DailyAttendanceShiftLookupDto
        {
            ShiftId = s.ShiftId,
            ShiftName = s.ShiftNameSnapshot,
            ScheduledStartAt = s.ScheduledStartAt,
            ScheduledEndAt = s.ScheduledEndAt
        }).ToList();

        // 3. Fetch assignments matching filters
        var assignments = await _attendanceRepo.GetDailyAssignmentsWithDetailsAsync(
            branchId,
            query.ScheduleWeekId,
            query.Date,
            query.Search,
            query.ShiftId,
            cancellationToken);

        // 4. Map to Item DTOs
        var activeRule = await _attendanceRepo.GetActiveAttendanceRuleAsync(cancellationToken);
        int defaultCheckOutAfterMinutes = activeRule?.CheckOutAfterMinutes ?? 60;
        var nowUtc = DateTime.UtcNow;
        var allItems = assignments.Select(sa =>
        {
            bool isExcusedLeave = false;
            string? leaveReason = null;

            var approvedLeave = sa.LeaveRequestAssignments
                .Where(lra => lra.Request != null && lra.Request.Status == "APPROVED")
                .Select(lra => lra.Request)
                .FirstOrDefault();

            if (approvedLeave != null)
            {
                isExcusedLeave = true;
                leaveReason = approvedLeave.Reason;
            }

            var attStatus = DetermineAttendanceStatus(
                sa.Attendance,
                sa.Shift.ScheduledStartAt,
                sa.Shift.ScheduledEndAt,
                isExcusedLeave,
                defaultCheckOutAfterMinutes,
                nowUtc);
            var attendance = sa.Attendance;
            bool isAbsentStatus = string.Equals(attStatus, "ABSENT", StringComparison.OrdinalIgnoreCase);

            return new ManagerDailyAttendanceItemDto
            {
                AssignmentId = sa.AssignmentId,
                ShiftId = sa.ShiftId,
                ShiftName = sa.Shift.ShiftNameSnapshot,
                ScheduledStartAt = sa.Shift.ScheduledStartAt,
                ScheduledEndAt = sa.Shift.ScheduledEndAt,
                UserId = sa.UserId,
                FullName = sa.User.FullName,
                Email = sa.User.Email,
                AvatarUrl = sa.User.AvatarUrl,
                RoleName = sa.User.Role.RoleName,
                AttendanceId = attendance?.AttendanceId,
                CheckInTime = attendance?.CheckInTime,
                CheckOutTime = attendance?.CheckOutTime,
                ActualWorkingMinutes = isAbsentStatus ? 0 : (attendance?.ActualWorkingMinutes ?? 0),
                TotalLateMinutes = isAbsentStatus ? 0 : (attendance?.TotalLateMinutes ?? 0),
                ExcusedLateMinutes = isAbsentStatus ? 0 : (attendance?.ExcusedLateMinutes ?? 0),
                ChargeableLateMinutes = isAbsentStatus ? 0 : (attendance?.ChargeableLateMinutes ?? 0),
                TotalEarlyLeaveMinutes = isAbsentStatus ? 0 : (attendance?.TotalEarlyLeaveMinutes ?? 0),
                ExcusedEarlyLeaveMinutes = isAbsentStatus ? 0 : (attendance?.ExcusedEarlyLeaveMinutes ?? 0),
                ChargeableEarlyLeaveMinutes = isAbsentStatus ? 0 : (attendance?.ChargeableEarlyLeaveMinutes ?? 0),
                CheckInFaceVerified = attendance?.CheckInFaceVerified,
                CheckOutFaceVerified = attendance?.CheckOutFaceVerified,
                CheckInDistanceMeters = attendance?.CheckInDistanceMeters,
                CheckOutDistanceMeters = attendance?.CheckOutDistanceMeters,
                AttendanceStatus = attStatus,
                IsExcusedLeave = isExcusedLeave,
                LeaveReason = leaveReason
            };
        }).ToList();

        // 5. Compute Summary metrics BEFORE status filtering
        var summary = new ManagerDailyAttendanceSummaryDto
        {
            TotalScheduled = allItems.Count,
            PresentCount = allItems.Count(i => i.AttendanceStatus is "IN_PROGRESS" or "COMPLETED" or "LATE" or "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE"),
            InProgressCount = allItems.Count(i => i.AttendanceStatus == "IN_PROGRESS"),
            CompletedCount = allItems.Count(i => i.AttendanceStatus == "COMPLETED"),
            LateCount = allItems.Count(i => i.ChargeableLateMinutes > 0 || i.AttendanceStatus is "LATE" or "LATE_AND_EARLY_LEAVE"),
            EarlyLeaveCount = allItems.Count(i => i.ChargeableEarlyLeaveMinutes > 0 || i.AttendanceStatus is "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE"),
            AbsentCount = allItems.Count(i => i.AttendanceStatus is "ABSENT" or "NOT_CHECKED_IN"),
            OnLeaveCount = allItems.Count(i => i.AttendanceStatus == "LEAVE")
        };

        // 6. Apply Status Filter
        if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var filterStatus = query.Status.Trim().ToUpperInvariant();
            if (filterStatus is "WORKING" or "IN_PROGRESS")
            {
                allItems = allItems.Where(i => i.AttendanceStatus == "IN_PROGRESS").ToList();
            }
            else if (filterStatus == "INCOMPLETE")
            {
                allItems = new List<ManagerDailyAttendanceItemDto>();
            }
            else if (filterStatus == "COMPLETED")
            {
                allItems = allItems.Where(i => i.AttendanceStatus == "COMPLETED").ToList();
            }
            else if (filterStatus == "LATE")
            {
                allItems = allItems.Where(i => i.ChargeableLateMinutes > 0 || i.AttendanceStatus is "LATE" or "LATE_AND_EARLY_LEAVE").ToList();
            }
            else if (filterStatus == "EARLY_LEAVE")
            {
                allItems = allItems.Where(i => i.ChargeableEarlyLeaveMinutes > 0 || i.AttendanceStatus is "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE").ToList();
            }
            else if (filterStatus == "ABSENT")
            {
                allItems = allItems.Where(i => i.AttendanceStatus is "ABSENT" or "NOT_CHECKED_IN").ToList();
            }
            else if (filterStatus == "LEAVE")
            {
                allItems = allItems.Where(i => i.AttendanceStatus == "LEAVE" || i.IsExcusedLeave).ToList();
            }
        }

        // 7. Pagination (fixed 20 items per page by default)
        int totalCount = allItems.Count;
        int pageSize = query.PageSize > 0 ? query.PageSize : 20;
        int pageIndex = Math.Max(1, query.PageIndex);

        var pagedItems = allItems
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ManagerDailyAttendanceResponse
        {
            BranchId = branchId,
            BranchName = manager.Branch?.BranchName ?? $"Branch {branchId}",
            SelectedScheduleWeekId = query.ScheduleWeekId,
            Date = query.Date,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Summary = summary,
            Items = pagedItems,
            AvailableWeeks = availableWeeks,
            AvailableShifts = availableShifts
        };
    }

    public async Task<ManagerAttendanceDetailResponse?> GetAttendanceDetailAsync(
        Guid managerId,
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        var manager = await _attendanceRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        var sa = await _attendanceRepo.GetAssignmentAttendanceDetailAsync(assignmentId, branchId, cancellationToken);
        if (sa == null)
        {
            return null;
        }

        var shift = sa.Shift;
        var week = shift.ScheduleWeek;
        var user = sa.User;
        var att = sa.Attendance;

        bool isExcusedLeave = false;
        string? leaveReason = null;
        AttendanceRequestSummaryDto? leaveRequestSummary = null;

        var approvedLeave = sa.LeaveRequestAssignments
            .Where(lra => lra.Request != null && lra.Request.Status == "APPROVED")
            .Select(lra => lra.Request)
            .FirstOrDefault();

        if (approvedLeave != null)
        {
            isExcusedLeave = true;
            leaveReason = approvedLeave.Reason;
            leaveRequestSummary = new AttendanceRequestSummaryDto
            {
                RequestId = approvedLeave.RequestId,
                RequestType = approvedLeave.RequestType,
                Status = approvedLeave.Status,
                Reason = approvedLeave.Reason,
                CreatedAt = approvedLeave.CreatedAt
            };
        }

        AttendanceRequestSummaryDto? lateReqSummary = null;
        if (att?.LateArrivalRequest != null)
        {
            var req = att.LateArrivalRequest.Request;
            lateReqSummary = new AttendanceRequestSummaryDto
            {
                RequestId = req.RequestId,
                RequestType = req.RequestType,
                Status = req.Status,
                Reason = req.Reason,
                RequestedTime = att.LateArrivalRequest.RequestedTime,
                CreatedAt = req.CreatedAt
            };
        }

        AttendanceRequestSummaryDto? earlyReqSummary = null;
        if (att?.EarlyLeaveRequest != null)
        {
            var req = att.EarlyLeaveRequest.Request;
            earlyReqSummary = new AttendanceRequestSummaryDto
            {
                RequestId = req.RequestId,
                RequestType = req.RequestType,
                Status = req.Status,
                Reason = req.Reason,
                RequestedTime = att.EarlyLeaveRequest.RequestedTime,
                CreatedAt = req.CreatedAt
            };
        }

        var activeRule = await _attendanceRepo.GetActiveAttendanceRuleAsync(cancellationToken);
        int defaultCheckOutAfterMinutes = activeRule?.CheckOutAfterMinutes ?? 60;
        var nowUtc = DateTime.UtcNow;
        var status = DetermineAttendanceStatus(
            att,
            shift.ScheduledStartAt,
            shift.ScheduledEndAt,
            isExcusedLeave,
            defaultCheckOutAfterMinutes,
            nowUtc);
        bool isAbsentStatus = string.Equals(status, "ABSENT", StringComparison.OrdinalIgnoreCase);

        return new ManagerAttendanceDetailResponse
        {
            AssignmentId = sa.AssignmentId,
            ShiftId = shift.ShiftId,
            ShiftName = shift.ShiftNameSnapshot,
            ScheduledStartAt = shift.ScheduledStartAt,
            ScheduledEndAt = shift.ScheduledEndAt,
            ScheduleWeekId = week.ScheduleWeekId,
            WeekPeriodDisplay = $"Week #{week.ScheduleWeekId} ({week.WeekStartDate:dd/MM/yyyy} – {week.WeekEndDate:dd/MM/yyyy})",
            BranchName = week.Branch.BranchName,
            BranchAddress = week.Branch.Address,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            RoleName = user.Role.RoleName,
            AttendanceId = att?.AttendanceId,
            Status = status,
            CheckInTime = att?.CheckInTime,
            CheckInLatitude = att?.CheckInLatitude,
            CheckInLongitude = att?.CheckInLongitude,
            CheckInDistanceMeters = att?.CheckInDistanceMeters,
            CheckInAccuracyMeters = att?.CheckInAccuracyMeters,
            CheckInFaceVerified = att?.CheckInFaceVerified,
            CheckInFaceConfidence = att?.CheckInFaceConfidence,
            CheckOutTime = att?.CheckOutTime,
            CheckOutLatitude = att?.CheckOutLatitude,
            CheckOutLongitude = att?.CheckOutLongitude,
            CheckOutDistanceMeters = att?.CheckOutDistanceMeters,
            CheckOutAccuracyMeters = att?.CheckOutAccuracyMeters,
            CheckOutFaceVerified = att?.CheckOutFaceVerified,
            CheckOutFaceConfidence = att?.CheckOutFaceConfidence,
            ActualWorkingMinutes = isAbsentStatus ? 0 : (att?.ActualWorkingMinutes ?? 0),
            TotalLateMinutes = isAbsentStatus ? 0 : (att?.TotalLateMinutes ?? 0),
            ExcusedLateMinutes = isAbsentStatus ? 0 : (att?.ExcusedLateMinutes ?? 0),
            ChargeableLateMinutes = isAbsentStatus ? 0 : (att?.ChargeableLateMinutes ?? 0),
            TotalEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.TotalEarlyLeaveMinutes ?? 0),
            ExcusedEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.ExcusedEarlyLeaveMinutes ?? 0),
            ChargeableEarlyLeaveMinutes = isAbsentStatus ? 0 : (att?.ChargeableEarlyLeaveMinutes ?? 0),
            IsExcusedLeave = isExcusedLeave,
            LeaveReason = leaveReason,
            LateArrivalRequest = lateReqSummary,
            EarlyLeaveRequest = earlyReqSummary,
            LeaveRequest = leaveRequestSummary,
            Penalties = att?.AttendancePenaltyRecords
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new AttendancePenaltyDto
                {
                    AttendancePenaltyId = p.AttendancePenaltyId,
                    ViolationType = p.ViolationType,
                    ViolationMinutes = p.ViolationMinutes,
                    PenaltyAmount = p.PenaltyAmount,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                }).ToList() ?? new()
        };
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

    private static string DetermineAttendanceStatus(
        Attendance? att,
        DateTime shiftStartAt,
        DateTime shiftEndAt,
        bool isExcusedLeave,
        int defaultCheckOutAfterMinutes,
        DateTime nowUtc)
    {
        if (isExcusedLeave)
        {
            return "LEAVE";
        }

        if (att != null)
        {
            if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
            {
                bool hasChargeableLate = att.ChargeableLateMinutes > 0;
                bool hasChargeableEarly = att.ChargeableEarlyLeaveMinutes > 0;

                if (hasChargeableLate && hasChargeableEarly)
                    return "LATE_AND_EARLY_LEAVE";
                if (hasChargeableLate)
                    return "LATE";
                if (hasChargeableEarly)
                    return "EARLY_LEAVE";
                return "COMPLETED";
            }

            if (string.Equals(att.Status, "ABSENT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(att.Status, "INCOMPLETE", StringComparison.OrdinalIgnoreCase))
            {
                return "ABSENT";
            }

            if (att.CheckInTime.HasValue && !att.CheckOutTime.HasValue)
            {
                var checkOutCutoffUtc = shiftEndAt.AddMinutes(GetCheckOutAfterMinutes(att, defaultCheckOutAfterMinutes));
                if (nowUtc > checkOutCutoffUtc)
                {
                    return "ABSENT";
                }
                return att.ChargeableLateMinutes > 0 ? "LATE" : "IN_PROGRESS";
            }

            if (shiftEndAt < nowUtc)
            {
                return "ABSENT";
            }

            return att.Status ?? "SCHEDULED";
        }

        if (shiftEndAt < nowUtc)
        {
            return "ABSENT";
        }

        if (shiftStartAt < nowUtc)
        {
            return "NOT_CHECKED_IN";
        }

        return "SCHEDULED";
    }
}
