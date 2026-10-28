namespace WorkShiftManagement.DAL.Entities;

public partial class AttendanceAdjustmentHistory
{
    public long AdjustmentHistoryId { get; set; }

    public int AttendanceId { get; set; }

    public Guid AdjustedBy { get; set; }

    public string AdjustmentSource { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime? OldCheckInTime { get; set; }

    public DateTime? NewCheckInTime { get; set; }

    public DateTime? OldCheckOutTime { get; set; }

    public DateTime? NewCheckOutTime { get; set; }

    public int? OldActualWorkingMinutes { get; set; }

    public int? NewActualWorkingMinutes { get; set; }

    public int? OldTotalLateMinutes { get; set; }

    public int? NewTotalLateMinutes { get; set; }

    public int? OldChargeableLateMinutes { get; set; }

    public int? NewChargeableLateMinutes { get; set; }

    public int? OldTotalEarlyLeaveMinutes { get; set; }

    public int? NewTotalEarlyLeaveMinutes { get; set; }

    public int? OldChargeableEarlyLeaveMinutes { get; set; }

    public int? NewChargeableEarlyLeaveMinutes { get; set; }

    public DateTime AdjustedAt { get; set; }

    public virtual User AdjustedByNavigation { get; set; } = null!;

    public virtual Attendance Attendance { get; set; } = null!;
}
