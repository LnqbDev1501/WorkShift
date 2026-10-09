namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

/// <summary>Filtering and pagination parameters for employee types.</summary>
public class EmployeeTypeListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
