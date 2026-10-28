namespace UI_Admin.Models.Manager.Attendance;

public sealed class ManagerDailyAttendanceViewModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? SelectedScheduleWeekId { get; set; }
    public DateOnly? Date { get; set; }
    public string? Search { get; set; }
    public string Status { get; set; } = "ALL";
    public int? ShiftId { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public ManagerDailyAttendanceSummaryViewModel Summary { get; set; } = new();
    public List<ManagerDailyAttendanceItemViewModel> Items { get; set; } = [];
    public List<DailyAttendanceShiftLookupViewModel> AvailableShifts { get; set; } = [];
    public List<DailyAttendanceWeekLookupViewModel> AvailableWeeks { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class DailyAttendanceWeekLookupViewModel
{
    public int ScheduleWeekId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsCurrentWeek { get; set; }
}

public sealed class ManagerDailyAttendanceSummaryViewModel
{
    public int TotalScheduled { get; set; }
    public int PresentCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyLeaveCount { get; set; }
    public int AbsentCount { get; set; }
    public int OnLeaveCount { get; set; }
}

public sealed class ManagerDailyAttendanceItemViewModel
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public int? AttendanceId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int ActualWorkingMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }

    public bool? CheckInFaceVerified { get; set; }
    public bool? CheckOutFaceVerified { get; set; }
    public decimal? CheckInDistanceMeters { get; set; }
    public decimal? CheckOutDistanceMeters { get; set; }

    public string AttendanceStatus { get; set; } = "SCHEDULED";
    public bool IsExcusedLeave { get; set; }
    public string? LeaveReason { get; set; }
}

public sealed class DailyAttendanceShiftLookupViewModel
{
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
}
