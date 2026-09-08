namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchEmployeesResponse
{
    public BranchSummaryResponse Branch { get; set; } = null!;
    public List<BranchEmployeeResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}
