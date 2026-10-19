using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class RespondShiftSwapRequest
{
    [StringLength(500)]
    public string? Note { get; set; }
}
