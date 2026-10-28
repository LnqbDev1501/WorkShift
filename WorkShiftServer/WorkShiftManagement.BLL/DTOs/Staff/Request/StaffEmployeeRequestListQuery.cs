namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class StaffEmployeeRequestListQuery
{
    public string? Reason { get; set; }

    public string? RequestType { get; set; }

    public string? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public bool SortByUpdatedAt { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
