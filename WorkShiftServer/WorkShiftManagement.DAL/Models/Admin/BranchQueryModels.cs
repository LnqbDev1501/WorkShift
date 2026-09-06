namespace WorkShiftManagement.DAL.Models.Admin;

public class BranchData
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
    public DateTime CreatedAt { get; set; }
}

public class BranchDetailData
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = null!;
    public string RegionName { get; set; } = null!;
    public string RegionStatus { get; set; } = null!;
    public bool RegionIsActive { get; set; }
    public string Address { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class BranchEmployeeData
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
}
