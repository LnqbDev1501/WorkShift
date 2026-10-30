using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class RejectEmployeeRequestRequest
{
    [Required(ErrorMessage = "Review note is required.")]
    [StringLength(500)]
    public string ReviewNote { get; set; } = null!;
}
