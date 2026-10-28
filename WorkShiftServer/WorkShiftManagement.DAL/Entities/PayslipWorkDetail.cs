namespace WorkShiftManagement.DAL.Entities;

public partial class PayslipWorkDetail
{
    public int PayslipWorkDetailId { get; set; }

    public int PayslipId { get; set; }

    public int SourceAssignmentId { get; set; }

    public int? SourceAttendanceId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string ShiftNameSnapshot { get; set; } = null!;

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }

    public int BreakMinutesSnapshot { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string AttendanceStatusSnapshot { get; set; } = null!;

    public int ActualWorkingMinutes { get; set; }

    public int PayableMinutes { get; set; }

    public int TotalLateMinutes { get; set; }

    public int ExcusedLateMinutes { get; set; }

    public int ChargeableLateMinutes { get; set; }

    public int TotalEarlyLeaveMinutes { get; set; }

    public int ExcusedEarlyLeaveMinutes { get; set; }

    public int ChargeableEarlyLeaveMinutes { get; set; }

    public string? Note { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payslip Payslip { get; set; } = null!;

    public virtual ShiftAssignment SourceAssignment { get; set; } = null!;

    public virtual Attendance? SourceAttendance { get; set; }
}
