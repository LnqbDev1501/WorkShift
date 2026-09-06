namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class BranchListQuery
{
    public string? Search { get; set; }
    public int? RegionId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
