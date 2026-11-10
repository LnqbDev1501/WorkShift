namespace WorkShiftManagement.DAL.ReadModels.Staff;

public sealed class StaffTimesheetWeekOptionReadModel
{
    public int ScheduleWeekId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public bool HasTimesheet { get; set; }
    public string Status { get; set; } = "";
}

public sealed class StaffTimesheetWeekReadModel
{
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string BranchName { get; set; } = "";
    public StaffPayrollPeriodReadModel? PayrollPeriod { get; set; }
    public List<StaffTimesheetAssignmentRow> Assignments { get; set; } = [];
}

public sealed class StaffPayrollPeriodReadModel
{
    public int PayrollPeriodId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StaffTimesheetReadModel> Timesheets { get; set; } = [];
}

public sealed class StaffTimesheetReadModel
{
    public int TimesheetId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "";
    public string StaffReviewStatus { get; set; } = "";
    public int TotalAssignedShifts { get; set; }
    public int CompletedShiftCount { get; set; }
    public int AbsentShiftCount { get; set; }
    public int IncompleteShiftCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public DateTime? ReviewDeadline { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public List<StaffTimesheetAdjustmentReadModel> TimesheetAdjustmentRequests { get; set; } = [];
}

public sealed class StaffTimesheetAssignmentRow
{
    public StaffTimesheetShiftReadModel Shift { get; set; } = new();
    public StaffTimesheetAssignmentReadModel Assignment { get; set; } = new();
}

public sealed class StaffTimesheetShiftReadModel
{
    public int ShiftId { get; set; }
    public string ShiftNameSnapshot { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
}

public sealed class StaffTimesheetAssignmentReadModel
{
    public int AssignmentId { get; set; }
    public string Status { get; set; } = "";
    public StaffAttendanceReadModel? Attendance { get; set; }
    public bool HasApprovedLeave { get; set; }
    public string? LeaveReason { get; set; }
}

public sealed class StaffAttendanceReadModel
{
    public int AttendanceId { get; set; }
    public string Status { get; set; } = "";
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int ActualWorkingMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int ChargeableLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }
    public int ChargeableEarlyLeaveMinutes { get; set; }
    public int? LateArrivalRequestId { get; set; }
    public int? EarlyLeaveRequestId { get; set; }
    public List<StaffAttendancePenaltyReadModel> AttendancePenaltyRecords { get; set; } = [];
}

public sealed class StaffAttendancePenaltyReadModel
{
    public int AttendancePenaltyId { get; set; }
    public string ViolationType { get; set; } = "";
    public int ViolationMinutes { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class StaffTimesheetAdjustmentReadModel
{
    public int AdjustmentRequestId { get; set; }
    public int AttendanceId { get; set; }
    public string Status { get; set; } = "";
    public bool AdjustCheckIn { get; set; }
    public bool AdjustCheckOut { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public string Reason { get; set; } = "";
    public string? ManagerNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

