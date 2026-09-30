using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class ReviewShiftCancellationRequest
{
    [StringLength(500)]
    public string? ReviewNote { get; set; }
}
