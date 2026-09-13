using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Holidays;

public sealed class HolidayListQueryViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be a positive whole number.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 9999, ErrorMessage = "Year must be between 1 and 9999.")]
    public int? Year { get; set; } = DateTime.Today.Year;

    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int? Month { get; set; }

    [StringLength(150, ErrorMessage = "Search cannot exceed 150 characters.")]
    public string? Search { get; set; }

    public bool IncludeInactive { get; set; }
}

public sealed class HolidayIndexViewModel
{
    public HolidayListQueryViewModel Query { get; set; } = new();
    public HolidayPageViewModel Result { get; set; } = new();
    public List<HolidayItemViewModel> SummaryItems { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public int FirstItem => Result.Items.Count == 0 ? 0 : (Result.PageNumber - 1) * Result.PageSize + 1;
    public int LastItem => Result.Items.Count == 0 ? 0 : FirstItem + Result.Items.Count - 1;
    public int ActiveCount => SummaryItems.Count(item => item.IsActive);
    public int InactiveCount => SummaryItems.Count(item => !item.IsActive);
    public decimal DefaultMultiplier => SummaryItems.Where(item => item.IsActive)
        .GroupBy(item => item.PayMultiplier)
        .OrderByDescending(group => group.Count())
        .ThenByDescending(group => group.Key)
        .Select(group => group.Key)
        .FirstOrDefault();
    public HolidayItemViewModel? UpcomingHoliday => SummaryItems
        .Where(item => item.IsActive && item.HolidayDate >= DateOnly.FromDateTime(DateTime.Today))
        .OrderBy(item => item.HolidayDate)
        .FirstOrDefault();
}

public sealed class HolidayPageViewModel
{
    public List<HolidayItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class HolidayItemViewModel
{
    public int PublicHolidayId { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public decimal PayMultiplier { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class HolidayFormItemViewModel
{
    [Required(ErrorMessage = "Holiday name is required.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "Holiday name must contain 1 to 150 characters.")]
    public string HolidayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Holiday date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? HolidayDate { get; set; }

    [Required(ErrorMessage = "Pay multiplier is required.")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "Pay multiplier must be between 0.01 and 999.99.")]
    public decimal? PayMultiplier { get; set; } = 3m;
}

public sealed class CreateHolidaysViewModel
{
    [MinLength(1, ErrorMessage = "Add at least one public holiday.")]
    [MaxLength(366, ErrorMessage = "A maximum of 366 holidays can be created at once.")]
    public List<HolidayFormItemViewModel> Holidays { get; set; } = [new()];
}

public sealed class EditHolidayViewModel
{
    public int PublicHolidayId { get; set; }
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Holiday name is required.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "Holiday name must contain 1 to 150 characters.")]
    public string HolidayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Holiday date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? HolidayDate { get; set; }

    [Required(ErrorMessage = "Pay multiplier is required.")]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "Pay multiplier must be between 0.01 and 999.99.")]
    public decimal? PayMultiplier { get; set; }
}
