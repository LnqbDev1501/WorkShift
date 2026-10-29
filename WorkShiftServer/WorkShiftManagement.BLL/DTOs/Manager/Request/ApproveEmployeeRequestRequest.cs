using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class ApproveEmployeeRequestRequest
{
    [StringLength(500)]
    public string? ReviewNote { get; set; }
}
