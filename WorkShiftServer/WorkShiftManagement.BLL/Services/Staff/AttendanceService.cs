using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Caching;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Attendance;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.BLL.Services.Staff;

public class AttendanceService : IAttendanceService
{
    private const int SessionTtlSeconds = 90;
    private const string ActionTypeCheckIn = "CHECK_IN";
    private const string ActionTypeCheckOut = "CHECK_OUT";
    private const int MaxImageSizeBytes = 10 * 1024 * 1024; // 10MB

    private static readonly TimeSpan VietnamTimeOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Converts a UTC DateTime to Vietnam Local Time (UTC+7).
    /// </summary>
    private static DateTime ToVietnamTime(DateTime utcTime) => utcTime.Add(VietnamTimeOffset);

    /// <summary>
    /// Converts a UTC DateTime to Vietnam DateOnly.
    /// </summary>
    private static DateOnly ToVietnamDate(DateTime utcTime) => DateOnly.FromDateTime(ToVietnamTime(utcTime));

    private static readonly (string Action, string Instruction)[] PossibleChallenges =
    {
        ("BLINK", "Please blink your eyes"),
        ("TURN_LEFT", "Please turn your face to the left"),
        ("TURN_RIGHT", "Please turn your face to the right"),
        ("SMILE", "Please smile"),
        ("NOD", "Please nod your head slightly")
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IAttendanceRepo _attendanceRepo;
    private readonly IFaceBiometricService _faceBiometricService;
    private readonly ILogger<AttendanceService> _logger;
    private readonly IMemoryCache? _memoryCache;

    public AttendanceService(
        IAttendanceRepo attendanceRepo,
        IFaceBiometricService faceBiometricService,
        ILogger<AttendanceService> logger,
        IMemoryCache? memoryCache = null)
    {
        _attendanceRepo = attendanceRepo;
        _faceBiometricService = faceBiometricService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    private async Task<CachedAttendanceRule?> GetCachedActiveAttendanceRuleAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache != null &&
            _memoryCache.TryGetValue(WorkShiftCacheKeys.ActiveAttendanceRule, out CachedAttendanceRule? cached) &&
            cached != null)
        {
            return cached;
        }

        var rule = await _attendanceRepo.GetActiveAttendanceRuleAsync(cancellationToken);
        if (rule == null)
        {
            return null;
        }

        var snapshot = new CachedAttendanceRule(
            rule.AttendanceRuleId,
            rule.RuleName,
            rule.CheckInBeforeMinutes,
            rule.CheckInGraceMinutes,
            rule.CheckOutGraceMinutes,
            rule.CheckOutAfterMinutes,
            rule.MaxAttendanceDistanceMeters,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.Status);

        _memoryCache?.Set(WorkShiftCacheKeys.ActiveAttendanceRule, snapshot, WorkShiftCacheKeys.DefaultTtl);
        return snapshot;
    }

    private async Task<CachedAttendancePenaltyRule?> GetCachedActiveAttendancePenaltyRuleAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache != null &&
            _memoryCache.TryGetValue(WorkShiftCacheKeys.ActiveAttendancePenaltyRule, out CachedAttendancePenaltyRule? cached) &&
            cached != null)
        {
            return cached;
        }

        var penaltyRule = await _attendanceRepo.GetActiveAttendancePenaltyRuleWithLevelsAsync(cancellationToken);
        if (penaltyRule == null)
        {
            return null;
        }

        var levels = Array.AsReadOnly(penaltyRule.AttendancePenaltyLevels
            .Select(l => new CachedAttendancePenaltyLevel(
                l.PenaltyLevelId,
                l.ViolationType,
                l.FromMinutes,
                l.ToMinutes,
                l.PenaltyAmount))
            .ToArray());

        var snapshot = new CachedAttendancePenaltyRule(
            penaltyRule.PenaltyRuleId,
            penaltyRule.RuleName,
            penaltyRule.EffectiveFrom,
            penaltyRule.EffectiveTo,
            penaltyRule.Status,
            levels);

        _memoryCache?.Set(WorkShiftCacheKeys.ActiveAttendancePenaltyRule, snapshot, WorkShiftCacheKeys.DefaultTtl);
        return snapshot;
    }

    private async Task<List<EligibleShiftResponse>> GetTodayShiftsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        // Validate user active + contract active + face profile registered
        await ValidateStaffPrerequisitesAsync(userId, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var todayVn = ToVietnamDate(nowUtc);

        var assignments = await _attendanceRepo.GetEligibleShiftAssignmentsForDateAsync(userId, todayVn, cancellationToken);
        if (assignments.Count == 0)
        {
            return new List<EligibleShiftResponse>();
        }

        var rule = await GetCachedActiveAttendanceRuleAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active attendance rule is configured in the system.");

        var assignmentIds = assignments.Select(a => a.AssignmentId).Distinct().ToList();
        var approvedLates = await _attendanceRepo.GetApprovedLateArrivalsForAssignmentsAsync(assignmentIds, cancellationToken);
        var approvedLeaves = await _attendanceRepo.GetApprovedLeavesForAssignmentsAsync(assignmentIds, cancellationToken);

        int checkInBeforeMins = rule.CheckInBeforeMinutes;
        int checkInGraceMins = rule.CheckInGraceMinutes;
        int maxDistanceMeters = rule.MaxAttendanceDistanceMeters;

        var results = new List<EligibleShiftResponse>();
        bool hasChanges = false;

        foreach (var assignment in assignments)
        {
            var shift = assignment.Shift;
            var shiftStartUtc = shift.ScheduledStartAt;
            var shiftEndUtc = shift.ScheduledEndAt;

            var windowOpensAtUtc = shiftStartUtc.AddMinutes(-checkInBeforeMins);
            int shiftDurationMinutes = Math.Max(1, (int)(shiftEndUtc - shiftStartUtc).TotalMinutes);
            int standardCutoffMinutes = Math.Min(120, shiftDurationMinutes / 2);
            var cutoffTimeUtc = shiftStartUtc.AddMinutes(standardCutoffMinutes);

            var approvedLate = approvedLates.GetValueOrDefault(assignment.AssignmentId);
            var approvedLeave = approvedLeaves.GetValueOrDefault(assignment.AssignmentId);

            if (approvedLate != null)
            {
                var lateCutoff = approvedLate.RequestedTime.AddMinutes(checkInGraceMins);
                if (lateCutoff > cutoffTimeUtc)
                {
                    cutoffTimeUtc = lateCutoff;
                }
            }

            bool hasApprovedLeave = approvedLeave != null;
            bool hasApprovedLate = approvedLate != null;
            int? approvedLateMins = hasApprovedLate
                ? Math.Max(0, (int)(approvedLate!.RequestedTime - shiftStartUtc).TotalMinutes)
                : null;

            bool hasCheckedIn = assignment.Attendance != null && assignment.Attendance.CheckInTime.HasValue;
            bool hasCheckedOut = assignment.Attendance != null && assignment.Attendance.CheckOutTime.HasValue;
            bool canCheckIn = true;
            string? cannotCheckInReason = null;
            bool canCheckOut = false;
            string? cannotCheckOutReason = null;
            var checkOutCutoffUtc = shiftEndUtc.AddMinutes(rule.CheckOutAfterMinutes);

            string? attendanceStatus = assignment.Attendance?.Status;

            if (!hasCheckedIn)
            {
                canCheckOut = false;
                cannotCheckOutReason = "You have not checked in for this shift yet.";

                if (nowUtc > checkOutCutoffUtc)
                {
                    // Ca đã qua giờ checkout luôn rồi mà không checkin: ABSENT
                    attendanceStatus = "ABSENT";
                }
            }
            else if (hasCheckedOut)
            {
                canCheckOut = false;
                cannotCheckOutReason = "You have already completed check-out for this shift.";
                attendanceStatus = assignment.Attendance?.Status ?? "COMPLETED";
            }
            else
            {
                // Has checked in but not checked out
                // Anti-accidental click (at least 2 minutes)
                if (nowUtc < assignment.Attendance!.CheckInTime!.Value.AddMinutes(2))
                {
                    canCheckOut = false;
                    cannotCheckOutReason = "Please wait at least 2 minutes after check-in before checking out.";
                    attendanceStatus = "IN_PROGRESS";
                }
                else
                {
                    if (nowUtc > checkOutCutoffUtc)
                    {
                        // Ca đã qua giờ checkout mà có checkin nhưng không checkout: INCOMPLETE
                        canCheckOut = false;
                        cannotCheckOutReason = $"Check-out window has closed. Cut-off was {ToVietnamTime(checkOutCutoffUtc):HH:mm}.";
                        attendanceStatus = "INCOMPLETE";
                        if (assignment.Attendance!.Status == "IN_PROGRESS")
                        {
                            assignment.Attendance.Status = "INCOMPLETE";
                            assignment.Attendance.UpdatedAt = DateTime.UtcNow;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        canCheckOut = true;
                        attendanceStatus = "IN_PROGRESS";
                    }
                }
            }

            if (hasCheckedIn)
            {
                canCheckIn = false;
                cannotCheckInReason = "You have already checked in for this shift.";
            }
            else if (hasApprovedLeave)
            {
                canCheckIn = false;
                cannotCheckInReason = "You have an approved leave request for this shift.";
            }
            else if (nowUtc < windowOpensAtUtc)
            {
                canCheckIn = false;
                cannotCheckInReason = $"Check-in window has not opened yet. Opens at {ToVietnamTime(windowOpensAtUtc):HH:mm}.";
            }
            else if (nowUtc > checkOutCutoffUtc)
            {
                canCheckIn = false;
                cannotCheckInReason = $"Check-out window has closed for this shift. Cut-off was {ToVietnamTime(checkOutCutoffUtc):HH:mm}.";
                attendanceStatus = "ABSENT";
            }
            else if (nowUtc > cutoffTimeUtc)
            {
                canCheckIn = false;
                cannotCheckInReason = $"Check-in window has closed for this shift. Cut-off time was {ToVietnamTime(cutoffTimeUtc):HH:mm}.";
                attendanceStatus = "ABSENT";
            }

            results.Add(new EligibleShiftResponse
            {
                AssignmentId = assignment.AssignmentId,
                ShiftId = shift.ShiftId,
                ShiftName = shift.ShiftNameSnapshot,
                ScheduledStartAt = shiftStartUtc,
                ScheduledEndAt = shiftEndUtc,
                BranchId = shift.ScheduleWeek.BranchId,
                BranchName = shift.ScheduleWeek.Branch.BranchName,
                BranchAddress = shift.ScheduleWeek.Branch.Address,
                BranchLatitude = shift.ScheduleWeek.Branch.Latitude,
                BranchLongitude = shift.ScheduleWeek.Branch.Longitude,
                CheckInWindowOpensAt = windowOpensAtUtc,
                CheckInCutoffAt = cutoffTimeUtc,
                CheckOutCutoffAt = checkOutCutoffUtc,
                CheckInGraceMinutes = checkInGraceMins,
                MaxAttendanceDistanceMeters = maxDistanceMeters,
                CanCheckInNow = canCheckIn,
                CanNotCheckInReason = cannotCheckInReason,
                HasApprovedLateArrival = hasApprovedLate,
                ApprovedLateArrivalTime = approvedLate?.RequestedTime,
                ApprovedLateMinutes = approvedLateMins,
                HasApprovedLeave = hasApprovedLeave,
                AssignmentStatus = assignment.Status,
                AttendanceStatus = attendanceStatus,
                HasCheckedIn = hasCheckedIn,
                HasCheckedOut = hasCheckedOut,
                CheckInTime = assignment.Attendance?.CheckInTime,
                CheckOutTime = assignment.Attendance?.CheckOutTime,
                CanCheckOutNow = canCheckOut,
                CanNotCheckOutReason = cannotCheckOutReason
            });
        }

        if (hasChanges)
        {
            await _attendanceRepo.SaveChangesAsync(cancellationToken);
        }

        return results;
    }

    public async Task<List<StaffAttendanceHistoryResponse>> GetStaffAttendanceHistoryAsync(
        Guid userId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        await ValidateStaffPrerequisitesAsync(userId, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var todayVn = ToVietnamDate(nowUtc);

        var effectiveToDate = toDate ?? todayVn;
        var effectiveFromDate = fromDate ?? todayVn.AddDays(-30);

        var assignments = await _attendanceRepo.GetShiftAssignmentsByDateRangeAsync(
            userId, effectiveFromDate, effectiveToDate, cancellationToken);

        var rule = await GetCachedActiveAttendanceRuleAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active attendance rule is configured in the system.");

        var assignmentIds = assignments.Select(a => a.AssignmentId).Distinct().ToList();
        var approvedLates = await _attendanceRepo.GetApprovedLateArrivalsForAssignmentsAsync(assignmentIds, cancellationToken);
        var approvedLeaves = await _attendanceRepo.GetApprovedLeavesForAssignmentsAsync(assignmentIds, cancellationToken);

        int checkOutAfterMinutes = rule.CheckOutAfterMinutes;
        int checkInBeforeMinutes = rule.CheckInBeforeMinutes;
        int checkInGraceMinutes = rule.CheckInGraceMinutes;
        int maxDistance = rule.MaxAttendanceDistanceMeters;

        var results = new List<StaffAttendanceHistoryResponse>();
        bool hasChanges = false;

        foreach (var assignment in assignments)
        {
            var shift = assignment.Shift;
            var shiftStartUtc = shift.ScheduledStartAt;
            var shiftEndUtc = shift.ScheduledEndAt;

            var checkoutCutoffUtc = shiftEndUtc.AddMinutes(checkOutAfterMinutes);

            var attendance = assignment.Attendance;
            bool hasCheckedIn = attendance != null && attendance.CheckInTime.HasValue;
            bool hasCheckedOut = attendance != null && attendance.CheckOutTime.HasValue;

            string status;
            bool canCheckIn = false;
            string? cannotCheckInReason = null;
            bool canCheckOut = false;
            string? cannotCheckOutReason = null;

            if (hasCheckedOut)
            {
                status = "COMPLETED";
                canCheckIn = false;
                cannotCheckInReason = "Shift check-out has already been completed.";
                canCheckOut = false;
                cannotCheckOutReason = "Shift check-out has already been completed.";
            }
            else if (hasCheckedIn)
            {
                canCheckIn = false;
                cannotCheckInReason = "Already checked in for this shift.";

                // Has checked in but not checked out
                if (nowUtc <= checkoutCutoffUtc)
                {
                    // Still within checkout window
                    status = "IN_PROGRESS";
                    // Anti-accidental click
                    if (nowUtc < attendance!.CheckInTime!.Value.AddMinutes(2))
                    {
                        canCheckOut = false;
                        cannotCheckOutReason = "Please wait at least 2 minutes after check-in before checking out.";
                    }
                    else
                    {
                        canCheckOut = true;
                    }
                }
                else
                {
                    // Checkout window has closed! Mark as INCOMPLETE
                    status = "INCOMPLETE";
                    canCheckOut = false;
                    cannotCheckOutReason = $"Check-out window has closed. Cut-off was {ToVietnamTime(checkoutCutoffUtc):HH:mm dd/MM/yyyy}.";

                    if (attendance!.Status == "IN_PROGRESS")
                    {
                        attendance.Status = "INCOMPLETE";
                        attendance.UpdatedAt = DateTime.UtcNow;
                        hasChanges = true;
                    }
                }
            }
            else
            {
                // Has not checked in
                canCheckOut = false;
                cannotCheckOutReason = "You have not checked in for this shift.";

                if (nowUtc > checkoutCutoffUtc)
                {
                    status = "ABSENT";
                    canCheckIn = false;
                    cannotCheckInReason = $"Shift is closed. Cut-off time was {ToVietnamTime(checkoutCutoffUtc):HH:mm dd/MM/yyyy}.";
                }
                else
                {
                    var approvedLeave = approvedLeaves.GetValueOrDefault(assignment.AssignmentId);
                    if (approvedLeave != null)
                    {
                        canCheckIn = false;
                        cannotCheckInReason = "You have an approved leave request for this shift.";
                        status = "ON_LEAVE";
                    }
                    else
                    {
                        var approvedLate = approvedLates.GetValueOrDefault(assignment.AssignmentId);
                        var windowOpensAtUtc = shiftStartUtc.AddMinutes(-checkInBeforeMinutes);
                        int shiftDurationMinutes = Math.Max(1, (int)(shiftEndUtc - shiftStartUtc).TotalMinutes);
                        int standardCutoffMinutes = Math.Min(120, shiftDurationMinutes / 2);
                        var cutoffTimeUtc = shiftStartUtc.AddMinutes(standardCutoffMinutes);

                        if (approvedLate != null)
                        {
                            var lateCutoff = approvedLate.RequestedTime.AddMinutes(checkInGraceMinutes);
                            if (lateCutoff > cutoffTimeUtc)
                            {
                                cutoffTimeUtc = lateCutoff;
                            }
                        }

                        if (nowUtc < windowOpensAtUtc)
                        {
                            canCheckIn = false;
                            cannotCheckInReason = $"Too early to check in. Check-in window opens at {ToVietnamTime(windowOpensAtUtc):HH:mm dd/MM/yyyy}.";
                            status = "UPCOMING";
                        }
                        else if (nowUtc > cutoffTimeUtc)
                        {
                            canCheckIn = false;
                            cannotCheckInReason = $"Check-in window closed at {ToVietnamTime(cutoffTimeUtc):HH:mm dd/MM/yyyy}. Shift recorded as Absent.";
                            status = "ABSENT";
                        }
                        else
                        {
                            canCheckIn = true;
                            status = "READY_TO_CHECK_IN";
                        }
                    }
                }
            }

            results.Add(new StaffAttendanceHistoryResponse
            {
                AttendanceId = attendance?.AttendanceId,
                AssignmentId = assignment.AssignmentId,
                ShiftId = shift.ShiftId,
                ShiftName = shift.ShiftNameSnapshot,
                ScheduledStartAt = shiftStartUtc,
                ScheduledEndAt = shiftEndUtc,
                BranchName = shift.ScheduleWeek.Branch.BranchName,
                BranchAddress = shift.ScheduleWeek.Branch.Address,
                CheckInTime = attendance?.CheckInTime,
                CheckOutTime = attendance?.CheckOutTime,
                ActualWorkingMinutes = attendance?.ActualWorkingMinutes ?? 0,
                Status = status,
                TotalLateMinutes = attendance?.TotalLateMinutes ?? 0,
                TotalEarlyLeaveMinutes = attendance?.TotalEarlyLeaveMinutes ?? 0,
                CanCheckInNow = canCheckIn,
                CanNotCheckInReason = cannotCheckInReason,
                CanCheckOutNow = canCheckOut,
                CannotCheckOutReason = cannotCheckOutReason,
                CheckOutCutoffAt = checkoutCutoffUtc,
                MaxAttendanceDistanceMeters = maxDistance,
                BranchLatitude = shift.ScheduleWeek.Branch.Latitude,
                BranchLongitude = shift.ScheduleWeek.Branch.Longitude
            });
        }

        if (hasChanges)
        {
            await _attendanceRepo.SaveChangesAsync(cancellationToken);
        }

        return results;
    }

    public async Task<StaffAttendanceDetailResponse?> GetStaffAttendanceDetailAsync(
        Guid userId, int assignmentId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        var sa = await _attendanceRepo.GetStaffAssignmentAttendanceDetailAsync(assignmentId, userId, cancellationToken);
        if (sa == null)
        {
            return null;
        }

        var shift = sa.Shift;
        var week = shift.ScheduleWeek;
        var att = sa.Attendance;

        var approvedLeave = sa.LeaveRequestAssignments
            ?.Select(l => l.Request)
            ?.FirstOrDefault(r => r != null && string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

        bool isExcusedLeave = approvedLeave != null;
        string? leaveReason = approvedLeave?.Reason;

        AttendanceRequestSummaryDto? lateReqSummary = null;
        if (att?.LateArrivalRequest?.Request != null)
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
        if (att?.EarlyLeaveRequest?.Request != null)
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

        AttendanceRequestSummaryDto? leaveRequestSummary = null;
        if (approvedLeave != null)
        {
            leaveRequestSummary = new AttendanceRequestSummaryDto
            {
                RequestId = approvedLeave.RequestId,
                RequestType = approvedLeave.RequestType,
                Status = approvedLeave.Status,
                Reason = approvedLeave.Reason,
                CreatedAt = approvedLeave.CreatedAt
            };
        }

        StaffTimesheetAdjustmentSummaryDto? timesheetAdjSummary = null;
        var adj = att?.TimesheetAdjustmentRequest;
        if (adj != null)
        {
            timesheetAdjSummary = new StaffTimesheetAdjustmentSummaryDto
            {
                AdjustmentRequestId = adj.AdjustmentRequestId,
                Status = adj.Status,
                OriginalCheckInTime = adj.CurrentCheckInSnapshot,
                OriginalCheckOutTime = adj.CurrentCheckOutSnapshot,
                RequestedCheckInTime = adj.RequestedCheckInTime,
                RequestedCheckOutTime = adj.RequestedCheckOutTime,
                AdjustCheckIn = adj.AdjustCheckIn,
                AdjustCheckOut = adj.AdjustCheckOut,
                Reason = adj.Reason,
                ManagerNote = adj.ManagerNote,
                ReviewedAt = adj.ReviewedAt,
                CreatedAt = adj.CreatedAt
            };
        }

        var nowUtc = DateTime.UtcNow;
        string status;
        if (isExcusedLeave)
        {
            status = "LEAVE";
        }
        else if (att != null)
        {
            if (att.CheckInTime.HasValue && !att.CheckOutTime.HasValue)
            {
                status = att.TotalLateMinutes > 0 ? "LATE" : "IN_PROGRESS";
            }
            else if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
            {
                status = "COMPLETED";
            }
            else
            {
                status = att.Status ?? "SCHEDULED";
            }
        }
        else
        {
            status = nowUtc > shift.ScheduledEndAt ? "ABSENT" : "SCHEDULED";
        }

        var rule = await GetCachedActiveAttendanceRuleAsync(cancellationToken);
        int maxDistance = rule?.MaxAttendanceDistanceMeters ?? 20;

        return new StaffAttendanceDetailResponse
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
            MaxAttendanceDistanceMeters = maxDistance,
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
            ActualWorkingMinutes = att?.ActualWorkingMinutes ?? 0,
            TotalLateMinutes = att?.TotalLateMinutes ?? 0,
            ExcusedLateMinutes = att?.ExcusedLateMinutes ?? 0,
            ChargeableLateMinutes = att?.ChargeableLateMinutes ?? 0,
            TotalEarlyLeaveMinutes = att?.TotalEarlyLeaveMinutes ?? 0,
            ExcusedEarlyLeaveMinutes = att?.ExcusedEarlyLeaveMinutes ?? 0,
            ChargeableEarlyLeaveMinutes = att?.ChargeableEarlyLeaveMinutes ?? 0,
            IsExcusedLeave = isExcusedLeave,
            LeaveReason = leaveReason,
            LateArrivalRequest = lateReqSummary,
            EarlyLeaveRequest = earlyReqSummary,
            LeaveRequest = leaveRequestSummary,
            TimesheetAdjustment = timesheetAdjSummary,
            Penalties = att?.AttendancePenaltyRecords?
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

    public async Task<EligibleShiftResponse?> GetEligibleShiftAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var shifts = await GetTodayShiftsAsync(userId, cancellationToken);
        if (shifts.Count == 0)
        {
            return null;
        }

        var nowUtc = DateTime.UtcNow;

        // Lọc các ca làm việc:
        // 1. Đã checkout thì loại bỏ
        // 2. Đã quá hạn checkout (CheckOutCutoffAt) thì loại bỏ
        // 3. Vẫn giữ các ca quá hạn check-in (ABSENT) để hiển thị lý do hết hạn cho nhân viên
        var activeOrUpcomingShifts = shifts.Where(s => 
            !s.HasCheckedOut && 
            nowUtc <= s.CheckOutCutoffAt
        ).ToList();

        if (activeOrUpcomingShifts.Count == 0)
        {
            return null;
        }

        // Tìm ca đang diễn ra chưa checkout (in-progress)
        var inProgressShift = activeOrUpcomingShifts.FirstOrDefault(s => s.HasCheckedIn && !s.HasCheckedOut);

        EligibleShiftResponse? mainShift = null;

        if (inProgressShift != null)
        {
            if (nowUtc < inProgressShift.ScheduledEndAt)
            {
                // Ca 1 vẫn đang trong giờ làm việc theo lịch -> Ca 1 vẫn là ca chính
                mainShift = inProgressShift;
            }
            else
            {
                // Ca 1 đã đến/qua giờ kết thúc theo lịch!
                // Tìm ca tiếp theo hôm nay (Ca 2)
                var nextShift = activeOrUpcomingShifts
                    .Where(s => s.AssignmentId != inProgressShift.AssignmentId)
                    .OrderBy(s => s.ScheduledStartAt)
                    .FirstOrDefault();

                if (nextShift != null)
                {
                    // Đẩy Ca 2 lên làm ca chính, đính kèm Ca 1 làm UnfinishedPreviousShift
                    nextShift.UnfinishedPreviousShift = inProgressShift;
                    mainShift = nextShift;
                }
                else
                {
                    // Không có ca nào khác tiếp theo hôm nay -> giữ Ca 1 cho đến khi hết hạn checkout
                    mainShift = inProgressShift;
                }
            }
        }
        else
        {
            // Không có ca in-progress: ưu tiên ca mở check-in ngay bây giờ, hoặc ca kế tiếp
            var openShift = activeOrUpcomingShifts.FirstOrDefault(s => s.CanCheckInNow);
            mainShift = openShift ?? activeOrUpcomingShifts
                .OrderBy(s => s.ScheduledStartAt)
                .FirstOrDefault();
        }

        return mainShift;
    }

    public async Task<CheckInSessionResponse> CreateCheckInSessionAsync(Guid userId, int? assignmentId = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        EligibleShiftResponse targetShift;
        if (assignmentId.HasValue && assignmentId.Value > 0)
        {
            var shifts = await GetTodayShiftsAsync(userId, cancellationToken);
            targetShift = shifts.FirstOrDefault(s => s.AssignmentId == assignmentId.Value)
                ?? throw new KeyNotFoundException($"Shift assignment '{assignmentId.Value}' was not found in your schedule today.");
        }
        else
        {
            targetShift = await GetEligibleShiftAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("You do not have any scheduled shift assigned for today.");
        }

        if (!targetShift.CanCheckInNow)
        {
            throw new InvalidOperationException(targetShift.CanNotCheckInReason ?? "You cannot check in for this shift at this time.");
        }

        if (targetShift.UnfinishedPreviousShift != null)
        {
            var prev = targetShift.UnfinishedPreviousShift;
            var startVn = ToVietnamTime(prev.ScheduledStartAt);
            var endVn = ToVietnamTime(prev.ScheduledEndAt);
            throw new InvalidOperationException($"You have an unfinished shift: '{prev.ShiftName}' ({startVn:HH:mm} – {endVn:HH:mm}) that has not been checked out. Please complete check-out in Attendance History before checking in for a new shift.");
        }

        // Invalidate any active pending session for this user
        var latestSession = await _attendanceRepo.GetLatestSessionByUserIdAsync(userId, ActionTypeCheckIn, cancellationToken);
        if (latestSession != null && latestSession.UsedAt == null && latestSession.CancelledAt == null && latestSession.ExpiresAt > DateTime.UtcNow)
        {
            latestSession.CancelledAt = DateTime.UtcNow;
            await _attendanceRepo.SaveChangesAsync(cancellationToken);
        }

        var challenges = GenerateRandomChallenges();
        var challengeSequence = JsonSerializer.Serialize(challenges.Select(c => c.Action).ToList());

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var nonceHash = ComputeNonceHash(sessionId, userId, now);

        var session = new FaceVerificationSession
        {
            SessionId = sessionId,
            UserId = userId,
            ActionType = ActionTypeCheckIn,
            FaceMatchVerified = false,
            FaceMatchScore = null,
            ChallengeSequence = challengeSequence,
            NonceHash = nonceHash,
            LivenessVerified = false,
            LivenessScore = null,
            ExpiresAt = now.AddSeconds(SessionTtlSeconds),
            UsedAt = null,
            CancelledAt = null,
            CreatedAt = now
        };

        await _attendanceRepo.CreateFaceVerificationSessionAsync(session, cancellationToken);

        _logger.LogInformation("Created face check-in session {SessionId} for user {UserId}", sessionId, userId);

        return new CheckInSessionResponse
        {
            SessionId = session.SessionId,
            ExpiresAt = session.ExpiresAt,
            ExpiresInSeconds = SessionTtlSeconds,
            ActionType = session.ActionType,
            Challenges = challenges
        };
    }

    public async Task<CheckInResponse> CheckInAsync(Guid userId, CheckInRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        if (request == null)
        {
            throw new ArgumentException("Check-in request payload is required.");
        }

        // 1. Validate Staff Prerequisites (Active User, Contract, Face Profile + Embedding)
        var (user, contract, faceProfile, faceEmbedding) = await ValidateStaffPrerequisitesWithEmbeddingAsync(userId, cancellationToken);

        // 2. Validate Assignment
        var assignment = await _attendanceRepo.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Shift assignment '{request.AssignmentId}' was not found.");

        if (assignment.UserId != userId)
        {
            throw new UnauthorizedAccessException("This shift assignment does not belong to your account.");
        }

        if (assignment.Status == "IN_PROGRESS" || assignment.Attendance != null)
        {
            throw new InvalidOperationException("You have already checked in for this shift assignment.");
        }

        if (assignment.Status != "ASSIGNED")
        {
            throw new InvalidOperationException($"Cannot check in. Shift assignment status is '{assignment.Status}'.");
        }

        var shift = assignment.Shift;
        if (shift.Status == "CANCELLED")
        {
            throw new InvalidOperationException("This work shift has been cancelled.");
        }

        // 3. Validate Approved Requests
        var approvedLeave = await _attendanceRepo.GetApprovedLeaveForAssignmentAsync(request.AssignmentId, cancellationToken);
        if (approvedLeave != null)
        {
            throw new InvalidOperationException("You have an approved leave request for this shift and cannot check in.");
        }

        var approvedLate = await _attendanceRepo.GetApprovedLateArrivalForAssignmentAsync(request.AssignmentId, cancellationToken);

        // 4. Validate Time Window & Cut-off
        var rule = await GetCachedActiveAttendanceRuleAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active attendance rule is configured in the system.");

        var penaltyRule = await GetCachedActiveAttendancePenaltyRuleAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var shiftStartUtc = shift.ScheduledStartAt;
        var shiftEndUtc = shift.ScheduledEndAt;

        // Prevent concurrent check-ins: User cannot check in if actively working in another shift whose checkout window hasn't expired
        var todayDate = ToVietnamDate(nowUtc);
        var todayAssignments = await _attendanceRepo.GetEligibleShiftAssignmentsForDateAsync(userId, todayDate, cancellationToken);
        var activeAttendance = todayAssignments.FirstOrDefault(a => 
        {
            if (a.AssignmentId == request.AssignmentId || a.Attendance == null || !a.Attendance.CheckInTime.HasValue || a.Attendance.CheckOutTime.HasValue)
                return false;

            var otherCheckoutCutoffUtc = a.Shift.ScheduledEndAt.AddMinutes(rule.CheckOutAfterMinutes);

            // Blocks only if the other shift is actively running right now (checkout window still open)
            return nowUtc <= otherCheckoutCutoffUtc;
        });

        if (activeAttendance != null)
        {
            var otherShift = activeAttendance.Shift;
            var startVn = ToVietnamTime(otherShift.ScheduledStartAt);
            var endVn = ToVietnamTime(otherShift.ScheduledEndAt);
            throw new InvalidOperationException($"You have an unfinished shift: '{otherShift.ShiftNameSnapshot}' ({startVn:HH:mm} – {endVn:HH:mm}) that has not been checked out. Please complete check-out in Attendance History before checking in for a new shift.");
        }

        var windowOpensAtUtc = shiftStartUtc.AddMinutes(-rule.CheckInBeforeMinutes);
        if (nowUtc < windowOpensAtUtc)
        {
            throw new InvalidOperationException($"Too early to check in. Check-in window opens at {ToVietnamTime(windowOpensAtUtc):HH:mm:ss dd/MM/yyyy} ({rule.CheckInBeforeMinutes} mins before shift start).");
        }

        int shiftDurationMinutes = Math.Max(1, (int)(shiftEndUtc - shiftStartUtc).TotalMinutes);
        int standardCutoffMinutes = Math.Min(120, shiftDurationMinutes / 2);
        var cutoffTimeUtc = shiftStartUtc.AddMinutes(standardCutoffMinutes);

        if (approvedLate != null)
        {
            var lateCutoff = approvedLate.RequestedTime.AddMinutes(rule.CheckInGraceMinutes);
            if (lateCutoff > cutoffTimeUtc)
            {
                cutoffTimeUtc = lateCutoff;
            }
        }

        if (nowUtc > cutoffTimeUtc)
        {
            throw new InvalidOperationException($"Check-in window has closed for this shift. Cut-off time was {ToVietnamTime(cutoffTimeUtc):HH:mm:ss dd/MM/yyyy}.");
        }

        // Calculate Late Minutes
        var normalGraceDeadlineUtc = shiftStartUtc.AddMinutes(rule.CheckInGraceMinutes);
        bool isLate = false;
        int totalLateMinutes = 0;
        int excusedLateMinutes = 0;
        int chargeableLateMinutes = 0;

        if (approvedLate != null)
        {
            var approvedGraceDeadlineUtc = approvedLate.RequestedTime.AddMinutes(rule.CheckInGraceMinutes);
            if (nowUtc <= approvedGraceDeadlineUtc)
            {
                // Arrived within approved late arrival window -> Exempted from penalty
                isLate = false;
                totalLateMinutes = Math.Max(0, (int)Math.Floor((nowUtc - shiftStartUtc).TotalMinutes));
                excusedLateMinutes = totalLateMinutes;
                chargeableLateMinutes = 0;
            }
            else
            {
                // Arrived later than approved time -> Loses exemption and penalized normally as unexcused late
                isLate = true;
                totalLateMinutes = Math.Max(0, (int)Math.Floor((nowUtc - shiftStartUtc).TotalMinutes));
                excusedLateMinutes = 0;
                chargeableLateMinutes = totalLateMinutes;
            }
        }
        else if (nowUtc > normalGraceDeadlineUtc)
        {
            // No approved request and arrived past normal grace period
            isLate = true;
            totalLateMinutes = Math.Max(0, (int)Math.Floor((nowUtc - shiftStartUtc).TotalMinutes));
            excusedLateMinutes = 0;
            chargeableLateMinutes = totalLateMinutes;
        }

        // 5. GPS Geofencing Validation (Strictly using rule.MaxAttendanceDistanceMeters)
        var branch = shift.ScheduleWeek.Branch;
        if (!branch.Latitude.HasValue || !branch.Longitude.HasValue)
        {
            throw new InvalidOperationException(
                $"Branch '{branch.BranchName}' does not have GPS coordinates configured. Please contact the administrator.");
        }

        double dist = CalculateHaversineDistance(
            (double)request.Latitude,
            (double)request.Longitude,
            (double)branch.Latitude.Value,
            (double)branch.Longitude.Value);

        decimal distanceMeters = Math.Round((decimal)dist, 2);

        // GPS Accuracy Compensation: Deduct accuracy (capped at 50m to prevent abuse/spoofing)
        decimal gpsAccuracy = Math.Max(0m, request.GpsAccuracy ?? 0m);
        decimal effectiveAccuracy = Math.Min(gpsAccuracy, 50m);
        decimal effectiveDistance = Math.Max(0m, distanceMeters - effectiveAccuracy);

        if (effectiveDistance > rule.MaxAttendanceDistanceMeters)
        {
            throw new InvalidOperationException(
                $"You are outside the branch check-in perimeter ({effectiveDistance:F1}m > {rule.MaxAttendanceDistanceMeters}m allowed [measured: {distanceMeters:F1}m, accuracy: ±{gpsAccuracy:F1}m]). Please move closer to {branch.BranchName}.");
        }

        // 6. Biometric Face Verification
        var session = await _attendanceRepo.GetFaceVerificationSessionAsync(request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Face verification session '{request.SessionId}' was not found.");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to use this verification session.");
        }

        if (session.ActionType != ActionTypeCheckIn)
        {
            throw new InvalidOperationException($"Invalid session action type '{session.ActionType}'. Expected '{ActionTypeCheckIn}'.");
        }

        if (session.CancelledAt.HasValue)
        {
            throw new InvalidOperationException("Face verification session has been cancelled. Please request a new session.");
        }

        if (session.UsedAt.HasValue)
        {
            throw new InvalidOperationException("Face verification session has already been used. Please request a new session.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Face verification session has expired. Please request a new session.");
        }

        ValidateImageFile(request.FaceImage);

        // Biometric ArcFace AI Verification
        var (currentEmbedding, faceConfidence) = await _faceBiometricService.ExtractFaceEmbeddingAsync(request.FaceImage, cancellationToken);
        var (isMatch, similarity) = _faceBiometricService.VerifyFaceMatch(faceEmbedding, currentEmbedding, threshold: 0.45f);

        if (!isMatch)
        {
            _logger.LogWarning("Face mismatch for user {UserId} at check-in. Similarity: {Similarity:P1} < 45.0%", userId, similarity);
            throw new InvalidOperationException($"Face verification failed: Your face does not match the registered profile (similarity: {similarity:P0} < 45%). Please face the camera clearly and try again.");
        }

        decimal qualityScore = Math.Round((decimal)faceConfidence, 4);
        decimal livenessScore = request.LivenessScore.HasValue
            ? Math.Round(request.LivenessScore.Value, 4)
            : 1.0000m;

        // 7. Serialize Rule Snapshot JSON
        var ruleSnapshotObj = new
        {
            AttendanceRule = new
            {
                rule.AttendanceRuleId,
                rule.RuleName,
                rule.CheckInBeforeMinutes,
                rule.CheckInGraceMinutes,
                rule.CheckOutGraceMinutes,
                rule.CheckOutAfterMinutes,
                rule.MaxAttendanceDistanceMeters,
                rule.EffectiveFrom,
                rule.EffectiveTo,
                rule.Status
            },
            AttendancePenaltyRule = penaltyRule != null ? new
            {
                penaltyRule.PenaltyRuleId,
                penaltyRule.RuleName,
                penaltyRule.EffectiveFrom,
                penaltyRule.EffectiveTo,
                penaltyRule.Status,
                PenaltyLevels = penaltyRule.Levels.Select(l => new
                {
                    l.PenaltyLevelId,
                    l.ViolationType,
                    l.FromMinutes,
                    l.ToMinutes,
                    l.PenaltyAmount
                }).ToList()
            } : null,
            SnapshottedAt = DateTime.UtcNow
        };
        string ruleSnapshotJson = JsonSerializer.Serialize(ruleSnapshotObj);

        // 8. Execute DB Transaction
        await using var transaction = await _attendanceRepo.BeginTransactionAsync(cancellationToken);
        try
        {
            nowUtc = DateTime.UtcNow;

            session.UsedAt = nowUtc;
            session.FaceMatchVerified = true;
            session.FaceMatchScore = (decimal)similarity;
            session.LivenessVerified = true;
            session.LivenessScore = livenessScore;
            session.MatchedFaceProfileId = faceProfile.FaceProfileId;

            var attendance = new Attendance
            {
                AssignmentId = assignment.AssignmentId,
                AttendanceRuleId = rule.AttendanceRuleId,
                CheckInFaceSessionId = session.SessionId,
                RuleSnapshotJson = ruleSnapshotJson,
                EmployeeFullNameSnapshot = user.FullName,
                ShiftStartAtSnapshot = shift.ScheduledStartAt,
                ShiftEndAtSnapshot = shift.ScheduledEndAt,
                CheckInTime = nowUtc,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                CheckInDistanceMeters = distanceMeters,
                CheckInAccuracyMeters = request.GpsAccuracy,
                BranchNameSnapshot = branch.BranchName,
                BranchAddressSnapshot = branch.Address,
                BranchLatitudeSnapshot = branch.Latitude,
                BranchLongitudeSnapshot = branch.Longitude,
                CheckInFaceVerified = true,
                CheckInFaceConfidence = qualityScore,
                LateArrivalRequestId = approvedLate?.RequestId,
                Status = "IN_PROGRESS",
                TotalLateMinutes = totalLateMinutes,
                ExcusedLateMinutes = excusedLateMinutes,
                ChargeableLateMinutes = chargeableLateMinutes,
                ActualWorkingMinutes = 0,
                TotalEarlyLeaveMinutes = 0,
                ExcusedEarlyLeaveMinutes = 0,
                ChargeableEarlyLeaveMinutes = 0,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            if (chargeableLateMinutes > 0 && penaltyRule != null)
            {
                var matchedLevel = penaltyRule.Levels
                    .FirstOrDefault(l => l.ViolationType == "LATE" &&
                                         chargeableLateMinutes >= l.FromMinutes &&
                                         (!l.ToMinutes.HasValue || chargeableLateMinutes <= l.ToMinutes.Value));

                if (matchedLevel != null)
                {
                    attendance.AttendancePenaltyRecords.Add(new AttendancePenaltyRecord
                    {
                        PenaltyRuleId = penaltyRule.PenaltyRuleId,
                        PenaltyLevelId = matchedLevel.PenaltyLevelId,
                        ViolationType = "LATE",
                        ViolationMinutes = chargeableLateMinutes,
                        PenaltyAmount = matchedLevel.PenaltyAmount,
                        Status = "APPLIED",
                        CreatedAt = nowUtc
                    });
                }
            }

            await _attendanceRepo.CreateAttendanceAsync(attendance, cancellationToken);

            assignment.UpdatedAt = nowUtc;

            await _attendanceRepo.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully checked in staff {UserId} for assignment {AssignmentId}, attendance {AttendanceId}. IsLate={IsLate}, ChargeableLate={LateMins}m",
                userId, assignment.AssignmentId, attendance.AttendanceId, isLate, chargeableLateMinutes);

            string message = isLate
                ? (chargeableLateMinutes > 0
                    ? $"Check-in successful with {totalLateMinutes} mins late ({chargeableLateMinutes} chargeable mins)."
                    : $"Check-in successful with {totalLateMinutes} mins late (fully excused by approved request).")
                : "Check-in successful! Have a great shift.";

            return new CheckInResponse
            {
                AttendanceId = attendance.AttendanceId,
                AssignmentId = assignment.AssignmentId,
                CheckInTime = attendance.CheckInTime!.Value,
                Status = attendance.Status,
                CheckInDistanceMeters = distanceMeters,
                IsLate = isLate,
                TotalLateMinutes = totalLateMinutes,
                ExcusedLateMinutes = excusedLateMinutes,
                ChargeableLateMinutes = chargeableLateMinutes,
                FaceMatchConfidence = qualityScore,
                Message = message
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to complete check-in transaction for user {UserId}, assignment {AssignmentId}", userId, request.AssignmentId);
            throw;
        }
    }

    public async Task<CheckOutSessionResponse> CreateCheckOutSessionAsync(Guid userId, int assignmentId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        if (assignmentId <= 0)
        {
            throw new ArgumentException("Valid AssignmentId is required.");
        }

        await ValidateStaffPrerequisitesAsync(userId, cancellationToken);

        var attendance = await _attendanceRepo.GetAttendanceWithDetailsByAssignmentIdAsync(assignmentId, cancellationToken);
        if (attendance == null)
        {
            throw new InvalidOperationException("Attendance record for this shift assignment was not found.");
        }

        if (attendance.Assignment.UserId != userId)
        {
            throw new UnauthorizedAccessException("This shift assignment does not belong to your account.");
        }

        if (attendance.CheckOutTime.HasValue || attendance.Status != "IN_PROGRESS")
        {
            throw new InvalidOperationException("You have already completed check-out for this shift.");
        }

        if (!attendance.CheckInTime.HasValue)
        {
            throw new InvalidOperationException("This shift does not have a check-in record and cannot be checked out.");
        }

        var shift = attendance.Assignment.Shift;
        var rule = await GetCachedActiveAttendanceRuleAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active attendance rule is configured in the system.");

        var nowUtc = DateTime.UtcNow;

        // Anti-accidental click guard (at least 2 minutes after check-in)
        if (nowUtc < attendance.CheckInTime.Value.AddMinutes(2))
        {
            throw new InvalidOperationException("You just checked in. Please wait at least 2 minutes before checking out.");
        }

        // Cut-off deadline validation
        var cutoffTimeUtc = shift.ScheduledEndAt.AddMinutes(rule.CheckOutAfterMinutes);
        if (nowUtc > cutoffTimeUtc)
        {
            throw new InvalidOperationException($"Check-out window has closed for this shift. Cut-off time was {ToVietnamTime(cutoffTimeUtc):HH:mm:ss dd/MM/yyyy}. Please contact your Manager.");
        }

        // Invalidate any active pending check-out session for this user
        var latestSession = await _attendanceRepo.GetLatestSessionByUserIdAsync(userId, ActionTypeCheckOut, cancellationToken);
        if (latestSession != null && latestSession.UsedAt == null && latestSession.CancelledAt == null && latestSession.ExpiresAt > DateTime.UtcNow)
        {
            latestSession.CancelledAt = DateTime.UtcNow;
            await _attendanceRepo.SaveChangesAsync(cancellationToken);
        }

        var challenges = GenerateRandomChallenges();
        var challengeSequence = JsonSerializer.Serialize(challenges.Select(c => c.Action).ToList());

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var nonceHash = ComputeNonceHash(sessionId, userId, now);

        var session = new FaceVerificationSession
        {
            SessionId = sessionId,
            UserId = userId,
            ActionType = ActionTypeCheckOut,
            FaceMatchVerified = false,
            FaceMatchScore = null,
            ChallengeSequence = challengeSequence,
            NonceHash = nonceHash,
            LivenessVerified = false,
            LivenessScore = null,
            ExpiresAt = now.AddSeconds(SessionTtlSeconds),
            UsedAt = null,
            CancelledAt = null,
            CreatedAt = now
        };

        await _attendanceRepo.CreateFaceVerificationSessionAsync(session, cancellationToken);

        _logger.LogInformation("Created face check-out session {SessionId} for user {UserId}, assignment {AssignmentId}", sessionId, userId, assignmentId);

        return new CheckOutSessionResponse
        {
            SessionId = session.SessionId,
            ExpiresAt = session.ExpiresAt,
            ExpiresInSeconds = SessionTtlSeconds,
            ActionType = session.ActionType,
            Challenges = challenges
        };
    }

    public async Task<CheckOutResponse> CheckOutAsync(Guid userId, CheckOutRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        if (request == null)
        {
            throw new ArgumentException("Check-out request payload is required.");
        }

        // 1. Validate Staff Prerequisites (Active User, Contract, Face Profile + Embedding)
        var (user, contract, faceProfile, faceEmbedding) = await ValidateStaffPrerequisitesWithEmbeddingAsync(userId, cancellationToken);

        // 2. Validate Attendance Session
        var attendance = await _attendanceRepo.GetAttendanceWithDetailsByAssignmentIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance record for assignment '{request.AssignmentId}' was not found.");

        var assignment = attendance.Assignment;
        if (assignment.UserId != userId)
        {
            throw new UnauthorizedAccessException("This attendance record does not belong to your account.");
        }

        if (attendance.CheckOutTime.HasValue || attendance.Status != "IN_PROGRESS")
        {
            throw new InvalidOperationException("You have already checked out for this shift.");
        }

        if (!attendance.CheckInTime.HasValue)
        {
            throw new InvalidOperationException("This shift assignment does not have an in-progress attendance session to check out.");
        }

        var shift = assignment.Shift;
        var branch = shift.ScheduleWeek.Branch;

        // Read snapshot from Check-in to guarantee rule immutability across the entire shift
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize RuleSnapshotJson for attendance {AttendanceId}. Fallback to active rules.", attendance.AttendanceId);
            }
        }

        // Active rule fallbacks if snapshot is missing
        var fallbackRule = snapshot?.AttendanceRule == null
            ? await GetCachedActiveAttendanceRuleAsync(cancellationToken)
            : null;
        int checkOutAfterMinutes = snapshot?.AttendanceRule?.CheckOutAfterMinutes ?? fallbackRule?.CheckOutAfterMinutes
            ?? throw new InvalidOperationException("No attendance rule configuration available for check-out.");
        int checkOutGraceMinutes = snapshot?.AttendanceRule?.CheckOutGraceMinutes ?? fallbackRule?.CheckOutGraceMinutes ?? 0;
        int maxDistanceMeters = snapshot?.AttendanceRule?.MaxAttendanceDistanceMeters ?? fallbackRule?.MaxAttendanceDistanceMeters ?? 100;

        int? penaltyRuleId = snapshot?.AttendancePenaltyRule?.PenaltyRuleId;
        List<AttendancePenaltyLevelSnapshotItem>? penaltyLevels = snapshot?.AttendancePenaltyRule?.PenaltyLevels;

        // If snapshot had no penalty rule, fallback to current active penalty rule
        if (penaltyRuleId == null || penaltyLevels == null)
        {
            var fallbackPenalty = await GetCachedActiveAttendancePenaltyRuleAsync(cancellationToken);
            if (fallbackPenalty != null)
            {
                penaltyRuleId = fallbackPenalty.PenaltyRuleId;
                penaltyLevels = fallbackPenalty.Levels.Select(l => new AttendancePenaltyLevelSnapshotItem
                {
                    PenaltyLevelId = l.PenaltyLevelId,
                    ViolationType = l.ViolationType,
                    FromMinutes = l.FromMinutes,
                    ToMinutes = l.ToMinutes,
                    PenaltyAmount = l.PenaltyAmount
                }).ToList();
            }
        }

        var nowUtc = DateTime.UtcNow;

        // Anti-accidental click guard
        if (nowUtc < attendance.CheckInTime.Value.AddMinutes(2))
        {
            throw new InvalidOperationException("You just checked in. Please wait at least 2 minutes before checking out.");
        }

        // Cut-off deadline validation
        var cutoffTimeUtc = shift.ScheduledEndAt.AddMinutes(checkOutAfterMinutes);
        if (nowUtc > cutoffTimeUtc)
        {
            throw new InvalidOperationException($"Check-out window has closed for this shift. Cut-off time was {ToVietnamTime(cutoffTimeUtc):HH:mm:ss dd/MM/yyyy}. Please contact your Manager.");
        }

        // 3. Early Leave Calculation
        var approvedEarly = await _attendanceRepo.GetApprovedEarlyLeaveForAssignmentAsync(assignment.AssignmentId, cancellationToken);
        var normalGraceDeadlineUtc = shift.ScheduledEndAt.AddMinutes(-checkOutGraceMinutes);

        bool isEarlyLeave = false;
        int totalEarlyLeaveMinutes = 0;
        int excusedEarlyLeaveMinutes = 0;
        int chargeableEarlyLeaveMinutes = 0;

        if (approvedEarly != null)
        {
            var approvedGraceDeadlineUtc = approvedEarly.RequestedTime.AddMinutes(-checkOutGraceMinutes);
            if (nowUtc >= approvedGraceDeadlineUtc)
            {
                // Left within approved early departure window -> Fully excused
                isEarlyLeave = false;
                totalEarlyLeaveMinutes = Math.Max(0, (int)Math.Ceiling((shift.ScheduledEndAt - nowUtc).TotalMinutes));
                excusedEarlyLeaveMinutes = totalEarlyLeaveMinutes;
                chargeableEarlyLeaveMinutes = 0;
            }
            else
            {
                // Left even earlier than approved early departure time -> Unexcused early leave penalty
                isEarlyLeave = true;
                totalEarlyLeaveMinutes = Math.Max(0, (int)Math.Ceiling((shift.ScheduledEndAt - nowUtc).TotalMinutes));
                excusedEarlyLeaveMinutes = 0;
                chargeableEarlyLeaveMinutes = totalEarlyLeaveMinutes;
            }
        }
        else if (nowUtc < normalGraceDeadlineUtc)
        {
            // Left earlier than normal grace window without approved request
            isEarlyLeave = true;
            totalEarlyLeaveMinutes = Math.Max(0, (int)Math.Ceiling((shift.ScheduledEndAt - nowUtc).TotalMinutes));
            excusedEarlyLeaveMinutes = 0;
            chargeableEarlyLeaveMinutes = totalEarlyLeaveMinutes;
        }

        // 4. GPS Geofencing Validation
        if (!branch.Latitude.HasValue || !branch.Longitude.HasValue)
        {
            throw new InvalidOperationException(
                $"Branch '{branch.BranchName}' does not have GPS coordinates configured. Please contact the administrator.");
        }

        double dist = CalculateHaversineDistance(
            (double)request.Latitude,
            (double)request.Longitude,
            (double)branch.Latitude.Value,
            (double)branch.Longitude.Value);

        decimal distanceMeters = Math.Round((decimal)dist, 2);

        // GPS Accuracy Compensation: Deduct accuracy (capped at 50m to prevent abuse/spoofing)
        decimal gpsAccuracy = Math.Max(0m, request.GpsAccuracy ?? 0m);
        decimal effectiveAccuracy = Math.Min(gpsAccuracy, 50m);
        decimal effectiveDistance = Math.Max(0m, distanceMeters - effectiveAccuracy);

        if (effectiveDistance > maxDistanceMeters)
        {
            throw new InvalidOperationException(
                $"You are outside the branch check-out perimeter ({effectiveDistance:F1}m > {maxDistanceMeters}m allowed [measured: {distanceMeters:F1}m, accuracy: ±{gpsAccuracy:F1}m]). Please move closer to {branch.BranchName}.");
        }

        // 5. Biometric Face Verification
        var session = await _attendanceRepo.GetFaceVerificationSessionAsync(request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Face verification session '{request.SessionId}' was not found.");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to use this verification session.");
        }

        if (session.ActionType != ActionTypeCheckOut)
        {
            throw new InvalidOperationException($"Invalid session action type '{session.ActionType}'. Expected '{ActionTypeCheckOut}'.");
        }

        if (session.CancelledAt.HasValue)
        {
            throw new InvalidOperationException("Face verification session has been cancelled. Please request a new session.");
        }

        if (session.UsedAt.HasValue)
        {
            throw new InvalidOperationException("Face verification session has already been used. Please request a new session.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Face verification session has expired. Please request a new session.");
        }

        ValidateImageFile(request.FaceImage);

        // Biometric ArcFace AI Verification
        var (currentEmbedding, faceConfidence) = await _faceBiometricService.ExtractFaceEmbeddingAsync(request.FaceImage, cancellationToken);
        var (isMatch, similarity) = _faceBiometricService.VerifyFaceMatch(faceEmbedding, currentEmbedding, threshold: 0.45f);

        if (!isMatch)
        {
            _logger.LogWarning("Face mismatch for user {UserId} at check-out. Similarity: {Similarity:P1} < 45.0%", userId, similarity);
            throw new InvalidOperationException($"Face verification failed: Your face does not match the registered profile (similarity: {similarity:P0} < 45%). Please face the camera clearly and try again.");
        }

        decimal qualityScore = Math.Round((decimal)faceConfidence, 4);
        decimal livenessScore = request.LivenessScore.HasValue
            ? Math.Round(request.LivenessScore.Value, 4)
            : 1.0000m;

        // 6. Calculate Actual Working Time
        int actualWorkingMinutes = Math.Max(0, (int)Math.Floor((nowUtc - attendance.CheckInTime.Value).TotalMinutes));
        decimal workingHours = Math.Round((decimal)actualWorkingMinutes / 60m, 2);

        // 7. Execute DB Transaction
        await using var transaction = await _attendanceRepo.BeginTransactionAsync(cancellationToken);
        try
        {
            session.UsedAt = nowUtc;
            session.FaceMatchVerified = true;
            session.FaceMatchScore = (decimal)similarity;
            session.LivenessVerified = true;
            session.LivenessScore = livenessScore;
            session.MatchedFaceProfileId = faceProfile.FaceProfileId;

            attendance.CheckOutTime = nowUtc;
            attendance.CheckOutLatitude = request.Latitude;
            attendance.CheckOutLongitude = request.Longitude;
            attendance.CheckOutDistanceMeters = distanceMeters;
            attendance.CheckOutAccuracyMeters = request.GpsAccuracy;
            attendance.CheckOutFaceSessionId = session.SessionId;
            attendance.CheckOutFaceVerified = true;
            attendance.CheckOutFaceConfidence = qualityScore;
            attendance.ActualWorkingMinutes = actualWorkingMinutes;
            attendance.EarlyLeaveRequestId = approvedEarly?.RequestId;

            bool hasLate = attendance.ChargeableLateMinutes > 0;
            bool hasEarly = chargeableEarlyLeaveMinutes > 0;

            if (hasLate && hasEarly)
            {
                attendance.Status = "LATE_AND_EARLY_LEAVE";
            }
            else if (hasLate)
            {
                attendance.Status = "LATE";
            }
            else if (hasEarly)
            {
                attendance.Status = "EARLY_LEAVE";
            }
            else
            {
                attendance.Status = "PRESENT";
            }

            attendance.TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes;
            attendance.ExcusedEarlyLeaveMinutes = excusedEarlyLeaveMinutes;
            attendance.ChargeableEarlyLeaveMinutes = chargeableEarlyLeaveMinutes;
            attendance.CompletedAt = nowUtc;
            attendance.UpdatedAt = nowUtc;

            if (chargeableEarlyLeaveMinutes > 0 && penaltyRuleId.HasValue && penaltyLevels != null)
            {
                var matchedLevel = penaltyLevels
                    .FirstOrDefault(l => l.ViolationType == "EARLY_LEAVE" &&
                                         chargeableEarlyLeaveMinutes >= l.FromMinutes &&
                                         (!l.ToMinutes.HasValue || chargeableEarlyLeaveMinutes <= l.ToMinutes.Value));

                if (matchedLevel != null)
                {
                    attendance.AttendancePenaltyRecords.Add(new AttendancePenaltyRecord
                    {
                        PenaltyRuleId = penaltyRuleId.Value,
                        PenaltyLevelId = matchedLevel.PenaltyLevelId,
                        ViolationType = "EARLY_LEAVE",
                        ViolationMinutes = chargeableEarlyLeaveMinutes,
                        PenaltyAmount = matchedLevel.PenaltyAmount,
                        Status = "APPLIED",
                        CreatedAt = nowUtc
                    });
                }
            }

            assignment.Status = "COMPLETED";
            assignment.UpdatedAt = nowUtc;

            await _attendanceRepo.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully checked out staff {UserId} for assignment {AssignmentId}, attendance {AttendanceId}. IsEarlyLeave={IsEarly}, ChargeableEarly={EarlyMins}m, ActualMinutes={Mins}",
                userId, assignment.AssignmentId, attendance.AttendanceId, isEarlyLeave, chargeableEarlyLeaveMinutes, actualWorkingMinutes);

            string message = isEarlyLeave
                ? (chargeableEarlyLeaveMinutes > 0
                    ? $"Check-out successful with {totalEarlyLeaveMinutes} mins early leave ({chargeableEarlyLeaveMinutes} chargeable mins)."
                    : $"Check-out successful with {totalEarlyLeaveMinutes} mins early leave (fully excused by approved request).")
                : "Check-out successful! Thank you for your work.";

            return new CheckOutResponse
            {
                AttendanceId = attendance.AttendanceId,
                AssignmentId = assignment.AssignmentId,
                CheckInTime = attendance.CheckInTime.Value,
                CheckOutTime = attendance.CheckOutTime.Value,
                ActualWorkingMinutes = actualWorkingMinutes,
                WorkingHours = workingHours,
                Status = attendance.Status,
                CheckOutDistanceMeters = distanceMeters,
                IsEarlyLeave = isEarlyLeave,
                TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
                ExcusedEarlyLeaveMinutes = excusedEarlyLeaveMinutes,
                ChargeableEarlyLeaveMinutes = chargeableEarlyLeaveMinutes,
                FaceMatchConfidence = qualityScore,
                Message = message
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to complete check-out transaction for user {UserId}, assignment {AssignmentId}", userId, request.AssignmentId);
            throw;
        }
    }

    private async Task<(User user, EmploymentContract contract)> ValidateUserAndContractPrerequisitesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _attendanceRepo.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Your user account is not active or was not found.");

        var contract = await _attendanceRepo.GetLatestContractByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("You do not have an employment contract registered in the system.");

        var today = ToVietnamDate(DateTime.UtcNow);

        if (string.Equals(contract.Status, "TERMINATED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Your employment contract has been terminated. Please contact HR / Management.");
        }

        if (string.Equals(contract.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
            (contract.EndDate.HasValue && contract.EndDate.Value < today))
        {
            throw new InvalidOperationException("Your employment contract has expired. Please contact HR / Management.");
        }

        if (!string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Your employment contract is not active (Status: {contract.Status}). Please contact HR / Management.");
        }

        if (contract.StartDate > today)
        {
            throw new InvalidOperationException($"Your employment contract has not started yet (Start date: {contract.StartDate:yyyy-MM-dd}). Please contact HR / Management.");
        }

        return (user, contract);
    }

    private async Task<(User user, EmploymentContract contract, FaceProfile profile)> ValidateStaffPrerequisitesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (user, contract) = await ValidateUserAndContractPrerequisitesAsync(userId, cancellationToken);

        var faceProfile = await _attendanceRepo.GetActiveFaceProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("You must register your face profile before performing attendance check-in.");

        return (user, contract, faceProfile);
    }

    private async Task<(User user, EmploymentContract contract, FaceProfile profile, float[] embedding)> ValidateStaffPrerequisitesWithEmbeddingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (user, contract) = await ValidateUserAndContractPrerequisitesAsync(userId, cancellationToken);

        var faceProfileData = await _attendanceRepo.GetActiveFaceProfileWithEmbeddingByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("You must register your face profile before performing attendance check-in.");

        return (user, contract, faceProfileData.Profile, faceProfileData.Embedding);
    }

    private static void ValidateImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("A live captured face image is required for biometric check-in.");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            throw new ArgumentException($"Face image exceeds the maximum permitted size of 10MB (file size: {file.Length / 1024 / 1024:F1}MB).");
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedImageExtensions.Contains(ext))
        {
            throw new ArgumentException($"Unsupported image format '{ext}'. Allowed formats: JPG, JPEG, PNG, WEBP.");
        }
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000.0;
        var dLat = (lat2 - lat1) * (Math.PI / 180.0);
        var dLon = (lon2 - lon1) * (Math.PI / 180.0);
        var rLat1 = lat1 * (Math.PI / 180.0);
        var rLat2 = lat2 * (Math.PI / 180.0);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusMeters * c;
    }

    private static List<FaceChallengeItemDto> GenerateRandomChallenges()
    {
        var random = Random.Shared;
        var selected = PossibleChallenges.OrderBy(_ => random.Next()).Take(2).ToList();

        var result = new List<FaceChallengeItemDto>();
        int order = 1;

        foreach (var (action, instruction) in selected)
        {
            result.Add(new FaceChallengeItemDto
            {
                Order = order++,
                Action = action,
                Instruction = instruction
            });
        }

        result.Add(new FaceChallengeItemDto
        {
            Order = order,
            Action = "LOOK_STRAIGHT",
            Instruction = "Look straight at the camera and hold still"
        });

        return result;
    }

    private static string ComputeNonceHash(Guid sessionId, Guid userId, DateTime timestamp)
    {
        var raw = $"{sessionId:N}:{userId:N}:{timestamp:O}:checkin-salt";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
