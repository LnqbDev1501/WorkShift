using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Manager.Attendance;

public sealed class ManagerDailyAttendanceResponse
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? SelectedScheduleWeekId { get; set; }
    public DateOnly? Date { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public ManagerDailyAttendanceSummaryDto Summary { get; set; } = new();
    public List<ManagerDailyAttendanceItemDto> Items { get; set; } = [];
    public List<DailyAttendanceWeekLookupDto> AvailableWeeks { get; set; } = [];
    public List<DailyAttendanceShiftLookupDto> AvailableShifts { get; set; } = [];
}

public sealed class DailyAttendanceWeekLookupDto
{
    public int ScheduleWeekId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsCurrentWeek { get; set; }
}

public sealed class ManagerDailyAttendanceSummaryDto
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

public sealed class ManagerDailyAttendanceItemDto
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

    public string AttendanceStatus { get; set; } = "SCHEDULED"; // SCHEDULED, IN_PROGRESS, COMPLETED, LATE, EARLY_LEAVE, LATE_AND_EARLY_LEAVE, ABSENT, LEAVE
    public bool IsExcusedLeave { get; set; }
    public string? LeaveReason { get; set; }
}

public sealed class DailyAttendanceShiftLookupDto
{
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
}
