using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class EligibleShiftResponse
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public int BreakMinutes { get; set; }

    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string BranchAddress { get; set; } = null!;
    public decimal? BranchLatitude { get; set; }
    public decimal? BranchLongitude { get; set; }

    public DateTime CheckInWindowOpensAt { get; set; }
    public DateTime CheckInCutoffAt { get; set; }
    public int CheckInGraceMinutes { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }

    public bool CanCheckInNow { get; set; }
    public string? CanNotCheckInReason { get; set; }

    public bool HasApprovedLateArrival { get; set; }
    public DateTime? ApprovedLateArrivalTime { get; set; }
    public int? ApprovedLateMinutes { get; set; }

    public bool HasApprovedLeave { get; set; }
}
