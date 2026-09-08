namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchRegionResponse
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = null!;
    public string RegionName { get; set; } = null!;
    public string Status { get; set; } = null!;
}
