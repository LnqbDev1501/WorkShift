using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class SubmitShiftSwapRequest
{
    [Range(1, int.MaxValue)]
    public int SourceAssignmentId { get; set; }
    [Range(1, int.MaxValue)]
    public int TargetAssignmentId { get; set; }
    [StringLength(500)]
    public string? Reason { get; set; }
}
