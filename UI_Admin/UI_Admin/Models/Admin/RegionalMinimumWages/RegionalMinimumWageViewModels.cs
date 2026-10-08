using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.RegionalMinimumWages;

public class RegionalMinimumWageListItemViewModel
{
    public int RegionalMinimumWageId { get; set; }
    public string WageName { get; set; } = string.Empty;
    public decimal MinimumHourlyRate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int? ReplacedWageId { get; set; }
}

public class RegionalMinimumWageDetailViewModel
{
    public int RegionalMinimumWageId { get; set; }
    public string WageName { get; set; } = string.Empty;
    public decimal MinimumHourlyRate { get; set; }
    public string? Description { get; set; }
    public int? ReplacedWageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRegionalMinimumWageViewModel
{
    [Required(ErrorMessage = "Wage Name is required.")]
    [StringLength(150, ErrorMessage = "Wage Name cannot exceed 150 characters.")]
    public string WageName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Minimum Hourly Rate is required.")]
    [Range(1, 100000000, ErrorMessage = "Minimum Hourly Rate must be greater than 0.")]
    public decimal MinimumHourlyRate { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public int? ReplacedWageId { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public List<RegionalMinimumWageListItemViewModel>? ActiveWages { get; set; }
}

public class UpdateRegionalMinimumWageViewModel
{
    public int RegionalMinimumWageId { get; set; }

    [Required(ErrorMessage = "Wage Name is required.")]
    [StringLength(150, ErrorMessage = "Wage Name cannot exceed 150 characters.")]
    public string WageName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Minimum Hourly Rate is required.")]
    [Range(1, 100000000, ErrorMessage = "Minimum Hourly Rate must be greater than 0.")]
    public decimal MinimumHourlyRate { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public int? ReplacedWageId { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public string? Status { get; set; }

    public List<RegionalMinimumWageListItemViewModel>? ActiveWages { get; set; }
}

public class RegionalMinimumWageListViewModel
{
    public List<RegionalMinimumWageListItemViewModel> Items { get; set; } = [];
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}
