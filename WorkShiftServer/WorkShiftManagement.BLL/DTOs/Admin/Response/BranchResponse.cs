namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchResponse
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = null!;
    public string RegionName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = null!;
}
