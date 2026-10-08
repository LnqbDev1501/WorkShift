using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Regions;

public class RegionalMinimumWageViewModel
{
    public int RegionalMinimumWageId { get; set; }
    public string WageName { get; set; } = string.Empty;
    public decimal MinimumHourlyRate { get; set; }
}

public class RegionListItemViewModel
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RegionalMinimumWageId { get; set; }
    public string? WageName { get; set; }
    public decimal? MinimumHourlyRate { get; set; }
    public int BranchCount { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class RegionListPagedViewModel
{
    public List<RegionListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateRegionViewModel
{
    [Required(ErrorMessage = "Region Code is required.")]
    [StringLength(20, ErrorMessage = "Region Code cannot exceed 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Region Code can only contain letters, numbers, hyphens or underscores.")]
    public string RegionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region Name is required.")]
    [StringLength(150, ErrorMessage = "Region Name cannot exceed 150 characters.")]
    public string RegionName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a regional minimum wage.")]
    public int? RegionalMinimumWageId { get; set; }

    public List<RegionalMinimumWageViewModel> MinimumWages { get; set; } = [];
}

public class EditRegionViewModel
{
    public int RegionId { get; set; }

    public string RegionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region Name is required.")]
    [StringLength(150, ErrorMessage = "Region Name cannot exceed 150 characters.")]
    public string RegionName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a regional minimum wage.")]
    public int? RegionalMinimumWageId { get; set; }

    public List<RegionalMinimumWageViewModel> MinimumWages { get; set; } = [];

    public string Status { get; set; } = "ACTIVE";

    public int BranchCount { get; set; }
}

public class RegionDetailViewModel
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public RegionalMinimumWageViewModel? MinimumWage { get; set; }
    public List<RegionBranchItemViewModel> Branches { get; set; } = [];
    public int ActiveBranchCount => Branches.Count(b => b.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase));
    public int InactiveBranchCount => Branches.Count(b => !b.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase));
}

public class RegionBranchItemViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
