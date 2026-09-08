using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateBranchRequest
{
    public string BranchCode { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public int RegionId { get; set; }
    public string Address { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must contain exactly 10 digits and start with 0.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }
}
