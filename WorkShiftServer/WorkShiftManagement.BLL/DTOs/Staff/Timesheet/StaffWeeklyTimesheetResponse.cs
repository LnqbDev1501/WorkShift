using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Staff.Timesheet;

public sealed class StaffWeeklyTimesheetResponse
{
    public int ScheduleWeekId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public int? TimesheetId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Status { get; set; } = null!; // "GENERATED", "NOT GENERATED", "LOCKED"
    public string StaffReviewStatus { get; set; } = null!; // "PENDING", "CONFIRMED", "-"
    public bool CanConfirm { get; set; }
    public bool IsLocked { get; set; }

    public int TotalAssignedShifts { get; set; }
    public int CompletedShifts { get; set; }
    public int AbsentShifts { get; set; }
    public int IncompleteShifts { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }

    public string TotalWorkTimeDisplay => $"{TotalWorkMinutes / 60}h {TotalWorkMinutes % 60}m";
    public string TotalLateDisplay => $"{TotalLateMinutes / 60}h {TotalLateMinutes % 60}m";
    public string TotalEarlyLeaveDisplay => $"{TotalEarlyLeaveMinutes / 60}h {TotalEarlyLeaveMinutes % 60}m";

    public List<StaffShiftAttendanceDto> Shifts { get; set; } = new();
    public List<StaffTimesheetWeekOptionDto> AvailableWeeks { get; set; } = new();
}

public sealed class StaffShiftAttendanceDto
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int ActualWorkingMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public string AttendanceStatus { get; set; } = null!;
    public string AssignmentStatus { get; set; } = null!;

    public string DateDisplay => ScheduledStartAt.AddHours(7).ToString("ddd, dd/MM/yyyy");
    public string ScheduledRangeDisplay => $"{ScheduledStartAt.AddHours(7):HH:mm} – {ScheduledEndAt.AddHours(7):HH:mm}";
    public string CheckInDisplay => CheckInTime.HasValue ? CheckInTime.Value.AddHours(7).ToString("HH:mm") : "-";
    public string CheckOutDisplay => CheckOutTime.HasValue ? CheckOutTime.Value.AddHours(7).ToString("HH:mm") : "-";
    public string WorkTimeDisplay => $"{ActualWorkingMinutes / 60}h {ActualWorkingMinutes % 60}m";
    public string LateDisplay => $"{TotalLateMinutes / 60}h {TotalLateMinutes % 60}m";
    public string EarlyLeaveDisplay => $"{TotalEarlyLeaveMinutes / 60}h {TotalEarlyLeaveMinutes % 60}m";
}

public sealed class StaffTimesheetWeekOptionDto
{
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public bool HasTimesheet { get; set; }
    public string Status { get; set; } = null!;
}

public sealed class StaffTimesheetConfirmRequest
{
    public int ScheduleWeekId { get; set; }
    public string? Note { get; set; }
}
