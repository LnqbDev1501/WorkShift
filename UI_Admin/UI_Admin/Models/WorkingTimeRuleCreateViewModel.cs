using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class WorkingTimeRuleCreateViewModel : IValidatableObject
{
    [Required, StringLength(150), Display(Name = "Version name / legal reference")]
    public string RuleName { get; set; } = string.Empty;

    [Required, Range(typeof(decimal), "0.25", "24"), Display(Name = "Maximum hours per day")]
    public decimal? MaxDailyHours { get; set; }

    [Required, Range(typeof(decimal), "0.25", "168"), Display(Name = "Maximum hours per week")]
    public decimal? MaxWeeklyHours { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (MaxDailyHours.HasValue && MaxDailyHours.Value % 0.25m != 0)
            yield return new("Daily hours must use 15-minute increments.", [nameof(MaxDailyHours)]);
        if (MaxWeeklyHours.HasValue && MaxWeeklyHours.Value % 0.25m != 0)
            yield return new("Weekly hours must use 15-minute increments.", [nameof(MaxWeeklyHours)]);
        if (MaxWeeklyHours < MaxDailyHours)
            yield return new("Weekly hours must be at least daily hours.", [nameof(MaxWeeklyHours)]);
    }
}
