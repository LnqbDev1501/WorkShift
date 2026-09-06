namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public sealed class ShiftTemplateListResponse
{
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public string Status { get; set; } = null!;
}
