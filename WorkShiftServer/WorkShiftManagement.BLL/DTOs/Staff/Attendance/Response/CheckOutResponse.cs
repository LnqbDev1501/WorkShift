using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class CheckOutResponse
{
    public int AttendanceId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime CheckOutTime { get; set; }
    public int ActualWorkingMinutes { get; set; }
    public decimal WorkingHours { get; set; }
    public string Status { get; set; } = "COMPLETED";
    public decimal CheckOutDistanceMeters { get; set; }
    public bool IsEarlyLeave { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }
    public int ChargeableEarlyLeaveMinutes { get; set; }
    public decimal? FaceMatchConfidence { get; set; }
    public string Message { get; set; } = null!;
}
