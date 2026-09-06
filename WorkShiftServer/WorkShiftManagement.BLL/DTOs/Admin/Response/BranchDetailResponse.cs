namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BranchDetailResponse
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public BranchRegionResponse Region { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = null!;
}
