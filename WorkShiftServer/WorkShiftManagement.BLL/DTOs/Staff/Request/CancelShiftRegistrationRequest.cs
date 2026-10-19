using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class CancelShiftRegistrationRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = "";
}
