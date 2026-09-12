using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public sealed class ShiftTemplateListQuery : IValidatableObject
{
    [Range(1, 20, ErrorMessage = "Page must be between 1 and 20.")]
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
