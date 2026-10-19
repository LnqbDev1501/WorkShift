using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

public sealed class ManagerTimesheetAdjustmentDto
{
    public int AdjustmentRequestId { get; set; }
    public int TimesheetId { get; set; }
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = null!;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = null!;
    public string? StaffEmail { get; set; }
    public string? StaffPhone { get; set; }
    public string RoleName { get; set; } = null!;

    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }

    public DateTime? OriginalCheckInTime { get; set; }
    public DateTime? OriginalCheckOutTime { get; set; }

    public bool AdjustCheckIn { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public bool AdjustCheckOut { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }

    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!; // "PENDING", "APPROVED", "REJECTED"
    public string? ManagerNote { get; set; }

    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Display helpers in Vietnam timezone (UTC+7)
    public string ShiftDateDisplay => ScheduledStartAt.AddHours(7).ToString("dd/MM/yyyy");
    public string ScheduledRangeDisplay => $"{ScheduledStartAt.AddHours(7):HH:mm} – {ScheduledEndAt.AddHours(7):HH:mm}";
    public string OriginalCheckInDisplay => OriginalCheckInTime.HasValue ? OriginalCheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string OriginalCheckOutDisplay => OriginalCheckOutTime.HasValue ? OriginalCheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string RequestedCheckInDisplay => RequestedCheckInTime.HasValue ? RequestedCheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string RequestedCheckOutDisplay => RequestedCheckOutTime.HasValue ? RequestedCheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";

    // Before-and-after audit history logs
    public List<AttendanceAdjustmentAuditHistoryDto> AuditHistories { get; set; } = new();
}

public sealed class AttendanceAdjustmentAuditHistoryDto
{
    public long AdjustmentHistoryId { get; set; }
    public int AttendanceId { get; set; }
    public Guid AdjustedBy { get; set; }
    public string? AdjustedByName { get; set; }
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

    public string AdjustedAtDisplay => AdjustedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    public string OldCheckInDisplay => OldCheckInTime.HasValue ? OldCheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string NewCheckInDisplay => NewCheckInTime.HasValue ? NewCheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string OldCheckOutDisplay => OldCheckOutTime.HasValue ? OldCheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string NewCheckOutDisplay => NewCheckOutTime.HasValue ? NewCheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
}

public sealed class ManagerTimesheetAdjustmentPagedResponse
{
    public List<ManagerTimesheetAdjustmentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
