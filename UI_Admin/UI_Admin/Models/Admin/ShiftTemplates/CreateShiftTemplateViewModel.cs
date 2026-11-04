using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace UI_Admin.Models;

public class CreateShiftTemplateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Shift name is required.")]
    [StringLength(150, ErrorMessage = "Shift name must not exceed 150 characters.")]
    [Display(Name = "Shift Name")]
    public string? ShiftName { get; set; }

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Use 24-hour HH:mm:ss format.")]
    [Display(Name = "Start Time (24h)")]
    public string? StartTime { get; set; }

    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Use 24-hour HH:mm:ss format.")]
    [Display(Name = "End Time (24h)")]
    public string? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TimeOnly.TryParseExact(StartTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && TimeOnly.TryParseExact(EndTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            if (end <= start)
                yield return new ValidationResult("End time must be later than start time on the same day. Overnight shifts are not supported.", [nameof(EndTime)]);
        }
    }
}
