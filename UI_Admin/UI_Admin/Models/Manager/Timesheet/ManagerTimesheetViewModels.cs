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
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public string StaffReviewStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
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
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }

    public List<ManagerStaffTimesheetItemViewModel> StaffTimesheets { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
