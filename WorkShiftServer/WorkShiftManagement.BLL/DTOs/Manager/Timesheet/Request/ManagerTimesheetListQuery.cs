namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;

public sealed class ManagerTimesheetListQuery
{
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public int? ScheduleWeekId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
