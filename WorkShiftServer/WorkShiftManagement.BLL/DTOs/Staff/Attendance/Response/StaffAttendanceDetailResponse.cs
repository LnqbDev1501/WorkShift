using System;
using System.Collections.Generic;
using WorkShiftManagement.BLL.DTOs.Manager.Attendance;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class StaffAttendanceDetailResponse
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public int ScheduleWeekId { get; set; }
    public string WeekPeriodDisplay { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? BranchAddress { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }

    public int? AttendanceId { get; set; }
    public string Status { get; set; } = "SCHEDULED";
    public DateTime? CheckInTime { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public decimal? CheckInDistanceMeters { get; set; }
    public decimal? CheckInAccuracyMeters { get; set; }
    public bool? CheckInFaceVerified { get; set; }
    public decimal? CheckInFaceConfidence { get; set; }

    public DateTime? CheckOutTime { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public decimal? CheckOutDistanceMeters { get; set; }
    public decimal? CheckOutAccuracyMeters { get; set; }
    public bool? CheckOutFaceVerified { get; set; }
    public decimal? CheckOutFaceConfidence { get; set; }

    public int ActualWorkingMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int ChargeableLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }
    public int ChargeableEarlyLeaveMinutes { get; set; }

    public bool IsExcusedLeave { get; set; }
    public string? LeaveReason { get; set; }

    // Associated Requests
    public AttendanceRequestSummaryDto? LateArrivalRequest { get; set; }
    public AttendanceRequestSummaryDto? EarlyLeaveRequest { get; set; }
    public AttendanceRequestSummaryDto? LeaveRequest { get; set; }

    // Associated Penalties
    public List<AttendancePenaltyDto> Penalties { get; set; } = new();

    // Associated Timesheet Adjustment Request
    public StaffTimesheetAdjustmentSummaryDto? TimesheetAdjustment { get; set; }
}

public sealed class StaffTimesheetAdjustmentSummaryDto
{
    public int AdjustmentRequestId { get; set; }
    public string Status { get; set; } = string.Empty; // PENDING, APPROVED, REJECTED
    public DateTime? OriginalCheckInTime { get; set; }
    public DateTime? OriginalCheckOutTime { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public bool AdjustCheckIn { get; set; }
    public bool AdjustCheckOut { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ManagerNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
