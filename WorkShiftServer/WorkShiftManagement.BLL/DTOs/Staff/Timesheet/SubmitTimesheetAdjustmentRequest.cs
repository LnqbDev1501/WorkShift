namespace WorkShiftManagement.BLL.DTOs.Staff.Timesheet;

public sealed class SubmitTimesheetAdjustmentRequest
{
    public int ScheduleWeekId { get; set; }
    public List<ShiftAdjustmentItemRequest> Adjustments { get; set; } = new();
}

public sealed class ShiftAdjustmentItemRequest
{
    public int AssignmentId { get; set; }
    public int? AttendanceId { get; set; }
    public bool AdjustCheckIn { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public bool AdjustCheckOut { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public string Reason { get; set; } = null!;
}
