namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class BranchEmployeeListQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
