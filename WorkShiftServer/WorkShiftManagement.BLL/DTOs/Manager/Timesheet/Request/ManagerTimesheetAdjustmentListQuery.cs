using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;

public sealed class ManagerTimesheetAdjustmentListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; } = "ALL"; // "ALL", "PENDING", "APPROVED", "REJECTED"
    public int? ScheduleWeekId { get; set; }
    public Guid? StaffId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
