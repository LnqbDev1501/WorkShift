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

    public bool IncludeInactive { get; set; } = true;
    public bool? IsActive { get; set; }
}

public sealed class HolidayIndexViewModel
{
    public HolidayListQueryViewModel Query { get; set; } = new();
    public HolidayPageViewModel Result { get; set; } = new();
    public List<HolidayItemViewModel> SummaryItems { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public int ActiveCount => SummaryItems.Count(item => item.IsActive);
    public int InactiveCount => SummaryItems.Count(item => !item.IsActive);
    public decimal? DefaultMultiplier => SummaryItems.Where(item => item.IsActive)
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
    public decimal? PayMultiplier { get; set; }
    public bool IsActive { get; set; }
    public bool IsPayrollLocked { get; set; }
    public List<int> AffectedPayrollPeriodIds { get; set; } = [];
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class HolidayFormItemViewModel
{
    [Required(ErrorMessage = "Holiday name is required.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "Holiday name must contain 1 to 150 characters.")]
    public string HolidayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "From date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? FromDate { get; set; }

    [Required(ErrorMessage = "To date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? ToDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class CreateHolidaysViewModel : IValidatableObject
{
    [MinLength(1, ErrorMessage = "Add at least one public holiday.")]
    [MaxLength(366, ErrorMessage = "A maximum of 366 holidays can be created at once.")]
    public List<HolidayFormItemViewModel> Holidays { get; set; } = [new()];

    public int TotalDays => Holidays.Sum(item => item.FromDate.HasValue && item.ToDate >= item.FromDate
        ? item.ToDate.Value.DayNumber - item.FromDate.Value.DayNumber + 1 : 0);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Holidays == null || Holidays.Count == 0)
        {
            yield return new ValidationResult("Add at least one holiday range.");
            yield break;
        }
        var dates = new HashSet<int>();
        var total = 0;
        foreach (var item in Holidays)
        {
            if (!item.FromDate.HasValue || !item.ToDate.HasValue) continue;
            if (item.FromDate == DateOnly.MinValue || item.ToDate < item.FromDate)
            {
                yield return new ValidationResult("To date must be on or after From date; use a valid start date.");
                yield break;
            }
            var days = item.ToDate.Value.DayNumber - item.FromDate.Value.DayNumber + 1;
            total += days;
            if (total > 366)
            {
                yield return new ValidationResult("A maximum of 366 days can be created at once.");
                yield break;
            }
            for (var offset = 0; offset < days; offset++)
                if (!dates.Add(item.FromDate.Value.DayNumber + offset))
                {
                    yield return new ValidationResult("Holiday ranges overlap. Each date must be unique.");
                    yield break;
                }
        }
    }
}

public sealed class EditHolidayViewModel
{
    public int PublicHolidayId { get; set; }

    [Required(ErrorMessage = "Holiday name is required.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "Holiday name must contain 1 to 150 characters.")]
    public string HolidayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Holiday date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? HolidayDate { get; set; }

    public bool IsActive { get; set; } = true;
}
