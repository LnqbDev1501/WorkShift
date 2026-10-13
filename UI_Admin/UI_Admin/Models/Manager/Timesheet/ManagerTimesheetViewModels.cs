using System;
using System.Collections.Generic;

namespace UI_Admin.Models.Manager.Timesheet;

public sealed class ManagerWeeklyTimesheetItemModel
{
    public int ScheduleWeekId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int TotalStaff { get; set; }
    public int ConfirmedStaff { get; set; }
    public string StaffReview { get; set; } = null!;
    public decimal ConfirmationRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class ManagerTimesheetPeriodOptionModel
{
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
}

public sealed class ManagerTimesheetPagedResponseModel<T>
{
    public List<T> Items { get; set; } = new();
    public List<ManagerTimesheetPeriodOptionModel> AvailablePeriods { get; set; } = new();
    public int TotalItems { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ManagerTimesheetListViewModel
{
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public int? ScheduleWeekId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public List<ManagerWeeklyTimesheetItemModel> Items { get; set; } = new();
    public List<ManagerTimesheetPeriodOptionModel> AvailablePeriods { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class ManagerStaffTimesheetItemViewModel
{
    public int? TimesheetId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? RoleName { get; set; }
    public int TotalAssignedShifts { get; set; }
    public int CompletedShifts { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public string StaffReviewStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<ManagerStaffShiftAttendanceViewModel> Shifts { get; set; } = new();

    public string TotalWorkTimeDisplay => $"{TotalWorkMinutes / 60}h {TotalWorkMinutes % 60}m";
    public string TotalLateDisplay => $"{TotalLateMinutes / 60}h {TotalLateMinutes % 60}m";
    public string TotalEarlyLeaveDisplay => $"{TotalEarlyLeaveMinutes / 60}h {TotalEarlyLeaveMinutes % 60}m";
}

public sealed class ManagerStaffShiftAttendanceViewModel
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

    public string DateDisplay => ScheduleDisplay.Vietnam(ScheduledStartAt).ToString("ddd, dd/MM/yyyy");
    public string ScheduledRangeDisplay => $"{ScheduleDisplay.Vietnam(ScheduledStartAt):HH:mm} – {ScheduleDisplay.Vietnam(ScheduledEndAt):HH:mm}";
    public string CheckInDisplay => CheckInTime.HasValue ? ScheduleDisplay.Vietnam(CheckInTime.Value).ToString("HH:mm") : "-";
    public string CheckOutDisplay => CheckOutTime.HasValue ? ScheduleDisplay.Vietnam(CheckOutTime.Value).ToString("HH:mm") : "-";
    public string WorkTimeDisplay => $"{ActualWorkingMinutes / 60}h {ActualWorkingMinutes % 60}m";
    public string LateDisplay => $"{TotalLateMinutes / 60}h {TotalLateMinutes % 60}m";
    public string EarlyLeaveDisplay => $"{TotalEarlyLeaveMinutes / 60}h {TotalEarlyLeaveMinutes % 60}m";
}

public sealed class ManagerStaffTimesheetDetailViewModel
{
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string WeekStatus { get; set; } = null!;
    public ManagerStaffTimesheetItemViewModel Staff { get; set; } = null!;
}

public sealed class ManagerWeeklyTimesheetDetailViewModel
{
    public int ScheduleWeekId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool CanGenerateTimesheet => string.Equals(Status, "NOT GENERATED", StringComparison.OrdinalIgnoreCase);
    public bool IsLocked => string.Equals(Status, "LOCKED", StringComparison.OrdinalIgnoreCase);

    public int TotalStaff { get; set; }
    public int ConfirmedStaff { get; set; }
    public string StaffReview { get; set; } = null!;
    public decimal ConfirmationRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }

    public string TotalWorkTimeDisplay => $"{TotalWorkMinutes / 60}h {TotalWorkMinutes % 60}m";
    public string TotalLateDisplay => $"{TotalLateMinutes / 60}h {TotalLateMinutes % 60}m";
    public string TotalEarlyLeaveDisplay => $"{TotalEarlyLeaveMinutes / 60}h {TotalEarlyLeaveMinutes % 60}m";

    public List<ManagerStaffTimesheetItemViewModel> StaffTimesheets { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
