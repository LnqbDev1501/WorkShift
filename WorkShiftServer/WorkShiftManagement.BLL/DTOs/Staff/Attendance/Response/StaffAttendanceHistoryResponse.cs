namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class StaffAttendanceHistoryResponse
{
    public int? AttendanceId { get; set; }
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string BranchName { get; set; } = null!;
    public string BranchAddress { get; set; } = null!;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int ActualWorkingMinutes { get; set; }
    public string Status { get; set; } = null!;
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public bool CanCheckInNow { get; set; }
    public string? CanNotCheckInReason { get; set; }
    public bool CanCheckOutNow { get; set; }
    public string? CannotCheckOutReason { get; set; }
    public DateTime? CheckOutCutoffAt { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }
    public decimal? BranchLatitude { get; set; }
    public decimal? BranchLongitude { get; set; }
}
