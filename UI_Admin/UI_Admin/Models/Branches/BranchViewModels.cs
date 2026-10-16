using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Branches;

public class RegionOptionViewModel
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
}

public class BranchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BranchListItemViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsInactive => Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsClosed => Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase);
}

public class BranchListPagedViewModel
{
    public List<BranchListItemViewModel> Items { get; set; } = [];
    public List<RegionOptionViewModel> AvailableRegions { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 1;
    public string? Search { get; set; }
    public int? RegionId { get; set; }
    public string Status { get; set; } = "ALL";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

}

public class BranchDetailViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string RegionStatus { get; set; } = "ACTIVE";
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsInactive => Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsClosed => Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase);

    public List<BranchEmployeeItemViewModel> Employees { get; set; } = [];
}

public class BranchEmployeeItemViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "U";
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}

public class CreateBranchViewModel
{
    [Required(ErrorMessage = "Branch code is required.")]
    [StringLength(30, ErrorMessage = "Branch code cannot exceed 30 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Branch code can only contain letters, digits, hyphens or underscores.")]
    public string BranchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch name is required.")]
    [StringLength(150, ErrorMessage = "Branch name cannot exceed 150 characters.")]
    public string BranchName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid active region.")]
    public int RegionId { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must contain exactly 10 digits and start with 0.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
    public string? Email { get; set; }

    public List<RegionOptionViewModel> AvailableRegions { get; set; } = [];
}

public class EditBranchViewModel
{
    public int BranchId { get; set; }

    public string BranchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch name is required.")]
    [StringLength(150, ErrorMessage = "Branch name cannot exceed 150 characters.")]
    public string BranchName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid active region.")]
    public int RegionId { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must contain exactly 10 digits and start with 0.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
    public string? Email { get; set; }

    public string Status { get; set; } = "ACTIVE";
    public int EmployeeCount { get; set; }

    public List<RegionOptionViewModel> AvailableRegions { get; set; } = [];
}
