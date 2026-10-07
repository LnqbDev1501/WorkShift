using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.AttendancePenaltyRule;

public sealed class CreateAttendancePenaltyRuleViewModel
{
    [Required(ErrorMessage = "Tên quy tắc là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tên quy tắc không được vượt quá 150 ký tự.")]
    [Display(Name = "Tên rule")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu hiệu lực là bắt buộc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu hiệu lực")]
    public DateOnly? EffectiveFrom { get; set; }

    public List<CreateAttendancePenaltyLevelViewModel> PenaltyLevels { get; set; } = new();
}
