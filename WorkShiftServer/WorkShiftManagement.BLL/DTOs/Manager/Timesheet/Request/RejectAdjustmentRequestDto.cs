using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;

public sealed class RejectAdjustmentRequestDto
{
    [Required(ErrorMessage = "Rejection note is required.")]
    [MinLength(3, ErrorMessage = "Rejection note must be at least 3 characters.")]
    [MaxLength(500, ErrorMessage = "Rejection note cannot exceed 500 characters.")]
    public string ManagerNote { get; set; } = null!;
}
