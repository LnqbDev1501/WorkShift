namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class ShiftSwapListQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    // ALL, SENT, or RECEIVED.
    public string Direction { get; set; } = "ALL";
}
