namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateBranchRequest
{
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string Address { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
