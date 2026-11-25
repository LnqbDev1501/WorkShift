namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class ProductListQuery
{
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
