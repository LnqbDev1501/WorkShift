namespace WorkShiftManagement.DAL.Entities;

public partial class Timesheet
{
    public int TimesheetId { get; set; }

    public Guid UserId { get; set; }

    public int PayrollPeriodId { get; set; }

    public int TotalAssignedShifts { get; set; }

    public int CompletedShiftCount { get; set; }

    public int AbsentShiftCount { get; set; }

    public int TotalScheduledMinutes { get; set; }

    public int TotalWorkMinutes { get; set; }

    public int TotalLateMinutes { get; set; }

    public int ExcusedLateMinutes { get; set; }

    public int ChargeableLateMinutes { get; set; }

    public int TotalEarlyLeaveMinutes { get; set; }

    public int ExcusedEarlyLeaveMinutes { get; set; }

    public int ChargeableEarlyLeaveMinutes { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? GeneratedAt { get; set; }

    public DateTime? ReviewDeadline { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string StaffReviewStatus { get; set; } = null!;

    public DateTime? StaffReviewedAt { get; set; }

    public string? StaffReviewNote { get; set; }

    public int IncompleteShiftCount { get; set; }

    public Guid? LockedBy { get; set; }

    public string? ReviewNote { get; set; }

    public virtual User? LockedByNavigation { get; set; }

    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual ICollection<TimesheetAdjustmentRequest> TimesheetAdjustmentRequests { get; set; } = new List<TimesheetAdjustmentRequest>();

    public virtual User User { get; set; } = null!;
}
