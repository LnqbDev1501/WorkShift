using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

public sealed class ManagerWeeklyTimesheetSummaryResponse
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
    public int NoResponseStaff { get; set; }
    public string StaffReview { get; set; } = null!;
    public decimal ConfirmationRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class ManagerTimesheetPeriodOptionDto
{
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
}

public sealed class ManagerTimesheetPagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public List<ManagerTimesheetPeriodOptionDto> AvailablePeriods { get; set; } = new();
    public int TotalItems { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
}
