namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class StaffShiftListQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
