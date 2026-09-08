namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchSummaryResponse
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string RegionCode { get; set; } = null!;
    public string Status { get; set; } = null!;
}
