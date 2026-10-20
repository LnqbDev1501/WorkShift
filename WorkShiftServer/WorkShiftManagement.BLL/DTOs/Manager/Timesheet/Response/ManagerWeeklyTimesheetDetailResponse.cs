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
    public bool CanLockTimesheet { get; set; }
    public DateTime? ReviewDeadline { get; set; }
    public bool IsPastDeadline { get; set; }
    public int PendingAdjustmentCount { get; set; }
    public string? LockEligibilityMessage { get; set; }

    public int TotalStaff { get; set; }
    public int ConfirmedStaff { get; set; }
    public int NoResponseStaff { get; set; }
    public string StaffReview { get; set; } = null!;
    public decimal ConfirmationRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public int TotalWorkMinutes { get; set; }
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
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int LateCount { get; set; }
    public int EarlyLeaveCount { get; set; }
    public int ExcusedLeaveCount { get; set; }
    public int UnexcusedAbsentCount { get; set; }
    public string StaffReviewStatus { get; set; } = null!; // "CONFIRMED", "PENDING", "NO_RESPONSE", "ADJUSTMENT_PENDING", "-"
    public string Status { get; set; } = null!; // "GENERATED", "LOCKED", "NOT GENERATED"
    public bool CanReopenReview => TimesheetId.HasValue 
        && string.Equals(Status, "GENERATED", StringComparison.OrdinalIgnoreCase) 
        && string.Equals(StaffReviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase);
    public List<ManagerStaffShiftAttendanceDto> Shifts { get; set; } = new();
}

public sealed class ManagerStaffShiftAttendanceDto
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
    public string AttendanceStatus { get; set; } = null!; // "COMPLETED", "PRESENT", "ABSENT", "SCHEDULED"
    public string AssignmentStatus { get; set; } = null!;
    public bool IsExcusedLeave { get; set; }
    public string? LeaveReason { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int ChargeableLateMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }
    public int ChargeableEarlyLeaveMinutes { get; set; }
    public bool HasApprovedLateArrival { get; set; }
    public bool HasApprovedEarlyLeave { get; set; }
    public List<WorkShiftManagement.BLL.DTOs.Manager.Attendance.AttendancePenaltyDto> Penalties { get; set; } = new();
}
