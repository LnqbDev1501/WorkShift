using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class RejectContractChangeRequest
{
    [Required(ErrorMessage = "A rejection reason is required.")]
    [MaxLength(500, ErrorMessage = "The rejection reason cannot exceed 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}
