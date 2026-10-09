using System.ComponentModel.DataAnnotations;
namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class CancelShiftAssignmentRequest
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}
