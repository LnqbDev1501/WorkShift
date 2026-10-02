using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;

public sealed class UpdateAttendanceRuleRequest
{
    [Required(ErrorMessage = "Rule name is required.")]
    [StringLength(150, ErrorMessage = "Rule name cannot exceed 150 characters.")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Check-in before minutes is required.")]
    [Range(0, 120, ErrorMessage = "Check-in before minutes must be between 0 and 120 minutes.")]
    public int? CheckInBeforeMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Check-in grace minutes is required.")]
    [Range(0, 60, ErrorMessage = "Check-in grace minutes must be between 0 and 60 minutes.")]
    public int? CheckInGraceMinutes { get; set; } = 0;

    [Required(ErrorMessage = "Check-out grace minutes is required.")]
    [Range(0, 60, ErrorMessage = "Check-out grace minutes must be between 0 and 60 minutes.")]
    public int? CheckOutGraceMinutes { get; set; } = 0;

    [Required(ErrorMessage = "Check-out after minutes is required.")]
    [Range(0, 120, ErrorMessage = "Check-out after minutes must be between 0 and 120 minutes.")]
    public int? CheckOutAfterMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Maximum attendance distance is required.")]
    [Range(10, 1000, ErrorMessage = "Maximum attendance distance must be between 10 and 1000 meters.")]
    public int? MaxAttendanceDistanceMeters { get; set; } = 100;

    [Required(ErrorMessage = "Effective From date is required.")]
    [DataType(DataType.Date)]
    public DateOnly? EffectiveFrom { get; set; }
}
