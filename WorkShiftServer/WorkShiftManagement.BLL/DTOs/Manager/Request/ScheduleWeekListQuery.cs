namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class ScheduleWeekListQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Status { get; set; }
}
