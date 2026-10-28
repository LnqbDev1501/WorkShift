namespace WorkShiftManagement.BLL.DTOs.Manager.Attendance;

public sealed class ManagerDailyAttendanceQuery
{
    public int? ScheduleWeekId { get; set; }
    public DateOnly? Date { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; } = "ALL";
    public int? ShiftId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
