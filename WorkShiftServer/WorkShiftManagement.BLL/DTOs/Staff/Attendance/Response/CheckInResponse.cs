using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class CheckInResponse
{
    public int AttendanceId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime CheckInTime { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public decimal CheckInDistanceMeters { get; set; }
    public bool IsLate { get; set; }
    public int TotalLateMinutes { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int ChargeableLateMinutes { get; set; }
    public decimal? FaceMatchConfidence { get; set; }
    public string Message { get; set; } = null!;
}
