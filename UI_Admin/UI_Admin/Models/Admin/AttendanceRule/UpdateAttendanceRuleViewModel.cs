using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.AttendanceRule;

public sealed class UpdateAttendanceRuleViewModel
{
    public int AttendanceRuleId { get; set; }

    [Required(ErrorMessage = "Rule name is required.")]
    [StringLength(150, ErrorMessage = "Rule name cannot exceed 150 characters.")]
    [Display(Name = "Rule Name")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Check-in window is required.")]
    [Range(0, 120, ErrorMessage = "Check-in window must be between 0 and 120 minutes.")]
    [Display(Name = "Check-in Window (Minutes)")]
    public int? CheckInBeforeMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Check-in grace period is required.")]
    [Range(0, 60, ErrorMessage = "Check-in grace period must be between 0 and 60 minutes.")]
    [Display(Name = "Check-in Grace Period (Minutes)")]
    public int? CheckInGraceMinutes { get; set; } = 5;

    [Required(ErrorMessage = "Check-out window is required.")]
    [Range(0, 120, ErrorMessage = "Check-out window must be between 0 and 120 minutes.")]
    [Display(Name = "Check-out Window (Minutes)")]
    public int? CheckOutAfterMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Check-out grace period is required.")]
    [Range(0, 60, ErrorMessage = "Check-out grace period must be between 0 and 60 minutes.")]
    [Display(Name = "Check-out Grace Period (Minutes)")]
    public int? CheckOutGraceMinutes { get; set; } = 5;

    [Required(ErrorMessage = "Maximum GPS distance is required.")]
    [Range(1, 20, ErrorMessage = "Maximum GPS distance must be between 1 and 20 meters.")]
    [Display(Name = "Max GPS Radius (Meters)")]
    public int? MaxAttendanceDistanceMeters { get; set; } = 20;

    [Required(ErrorMessage = "Effective From date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Effective From Date")]
    public DateOnly? EffectiveFrom { get; set; }
}
