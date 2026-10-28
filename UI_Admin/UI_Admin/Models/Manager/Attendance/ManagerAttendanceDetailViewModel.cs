namespace UI_Admin.Models.Manager.Attendance;

public sealed class ManagerAttendanceDetailViewModel
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

    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;

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
    public AttendanceRequestSummaryViewModel? LateArrivalRequest { get; set; }
    public AttendanceRequestSummaryViewModel? EarlyLeaveRequest { get; set; }
    public AttendanceRequestSummaryViewModel? LeaveRequest { get; set; }

    public string? ErrorMessage { get; set; }

    public System.Collections.Generic.List<ManagerAttendancePenaltyItemViewModel> Penalties { get; set; } = new();
}

public sealed class ManagerAttendancePenaltyItemViewModel
{
    public int AttendancePenaltyId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public int ViolationMinutes { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Status { get; set; } = "APPLIED";
    public DateTime CreatedAt { get; set; }
}

public sealed class AttendanceRequestSummaryViewModel
{
    public int RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime? RequestedTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
