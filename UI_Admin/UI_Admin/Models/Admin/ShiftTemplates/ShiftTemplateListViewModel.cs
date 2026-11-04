using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class ShiftTemplateListQueryViewModel : IValidatableObject
{
    [Range(1, 20, ErrorMessage = "Page must be a whole number between 1 and 20.")]
    public int Page { get; set; } = 1;
    public string? Search { get; set; }

    [RegularExpression("^(ACTIVE|INACTIVE)$", ErrorMessage = "Status must be ACTIVE or INACTIVE.")]
    public string? Status { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime.HasValue && StartTime > EndTime)
            yield return new ValidationResult("Start time must not be later than end time.", [nameof(EndTime)]);
    }
}

public sealed class ShiftTemplateListViewModel
{
    public ShiftTemplateListQueryViewModel Query { get; set; } = new();
    public ShiftTemplatePage Result { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Query.Search) || !string.IsNullOrEmpty(Query.Status)
        || Query.StartTime.HasValue || Query.EndTime.HasValue;
    public int LastPage => Math.Min(20, Result.TotalPages);
}

public sealed class ShiftTemplatePage
{
    public List<ShiftTemplateListItem> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ShiftTemplateListItem
{
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
}
