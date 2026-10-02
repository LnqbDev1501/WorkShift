using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class CancelEmploymentContractRequest
{
    [Required(ErrorMessage = "Cancellation reason is required.")]
    public string Reason { get; set; } = string.Empty;
}
