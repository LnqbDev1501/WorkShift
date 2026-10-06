using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class SubmitLeaveRequestRequest
{
    [Required(ErrorMessage = "At least one shift assignment is required.")]
    public List<int> AssignmentIds { get; set; } = [];

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    public string Reason { get; set; } = null!;
}
