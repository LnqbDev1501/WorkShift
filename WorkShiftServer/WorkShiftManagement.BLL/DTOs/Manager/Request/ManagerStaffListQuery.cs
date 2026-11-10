namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class ManagerStaffListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public bool? HasContract { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
