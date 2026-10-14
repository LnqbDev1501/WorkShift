using System;
using System.Collections.Generic;

namespace UI_Admin.Models.Manager.TimesheetAdjustment;

public class ManagerTimesheetAdjustmentItemModel
{
    public int AdjustmentRequestId { get; set; }
    public int TimesheetId { get; set; }
    public int ScheduleWeekId { get; set; }
    public string Period { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? StaffEmail { get; set; }
    public string? StaffPhone { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }

    public DateTime? OriginalCheckInTime { get; set; }
    public DateTime? OriginalCheckOutTime { get; set; }

    public bool AdjustCheckIn { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public bool AdjustCheckOut { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "PENDING", "APPROVED", "REJECTED"
    public string? ManagerNote { get; set; }

    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string ShiftDateDisplay { get; set; } = string.Empty;
    public string ScheduledRangeDisplay { get; set; } = string.Empty;
    public string OriginalCheckInDisplay { get; set; } = "--:--";
    public string OriginalCheckOutDisplay { get; set; } = "--:--";
    public string RequestedCheckInDisplay { get; set; } = "--:--";
    public string RequestedCheckOutDisplay { get; set; } = "--:--";
}

public class ManagerTimesheetAdjustmentListViewModel
{
    public List<ManagerTimesheetAdjustmentItemModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    // Filters
    public string? Search { get; set; }
    public string Status { get; set; } = "ALL";
    public int? ScheduleWeekId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
