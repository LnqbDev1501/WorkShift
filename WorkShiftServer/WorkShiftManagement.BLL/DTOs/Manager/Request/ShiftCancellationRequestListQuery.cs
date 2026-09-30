namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class ShiftCancellationRequestListQuery
{
    public string? Status { get; set; } = "PENDING";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
