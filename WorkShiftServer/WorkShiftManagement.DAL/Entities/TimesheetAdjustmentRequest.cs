namespace WorkShiftManagement.DAL.Entities;

public partial class TimesheetAdjustmentRequest
{
    public int AdjustmentRequestId { get; set; }

    public int TimesheetId { get; set; }

    public int AttendanceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? CurrentCheckInSnapshot { get; set; }

    public DateTime? CurrentCheckOutSnapshot { get; set; }

    public bool AdjustCheckIn { get; set; }

    public DateTime? RequestedCheckInTime { get; set; }

    public bool AdjustCheckOut { get; set; }

    public DateTime? RequestedCheckOutTime { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ManagerNote { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Attendance Attendance { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual Timesheet Timesheet { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
