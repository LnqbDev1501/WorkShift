using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.AttendancePenaltyRule;

public sealed class CreateAttendancePenaltyRuleViewModel
{
    [Required(ErrorMessage = "Rule name is required.")]
    [StringLength(150, ErrorMessage = "Rule name must not exceed 150 characters.")]
    [Display(Name = "Rule Name")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Effective from date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Effective From")]
    public DateOnly? EffectiveFrom { get; set; }

    public List<CreateAttendancePenaltyLevelViewModel> PenaltyLevels { get; set; } = new();
}
