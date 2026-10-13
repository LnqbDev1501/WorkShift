using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

public sealed class ManagerWeeklyTimesheetDetailResponse
{
    public int ScheduleWeekId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string Period { get; set; } = null!;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Status { get; set; } = null!; // "NOT GENERATED", "GENERATED", "LOCKED"
    public bool CanGenerateTimesheet => string.Equals(Status, "NOT GENERATED", StringComparison.OrdinalIgnoreCase);
    public bool IsLocked => string.Equals(Status, "LOCKED", StringComparison.OrdinalIgnoreCase);

    public int TotalStaff { get; set; }
    public int ConfirmedStaff { get; set; }
    public string StaffReview { get; set; } = null!;
    public decimal ConfirmationRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }

    public List<ManagerStaffTimesheetItemDto> StaffTimesheets { get; set; } = new();
}

public sealed class ManagerStaffTimesheetItemDto
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
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public string StaffReviewStatus { get; set; } = null!; // "CONFIRMED", "PENDING", "-"
    public string Status { get; set; } = null!; // "GENERATED", "LOCKED", "NOT GENERATED"
}
