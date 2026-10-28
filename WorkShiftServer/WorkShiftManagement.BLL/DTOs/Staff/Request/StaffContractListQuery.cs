namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class StaffContractListQuery
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
}
