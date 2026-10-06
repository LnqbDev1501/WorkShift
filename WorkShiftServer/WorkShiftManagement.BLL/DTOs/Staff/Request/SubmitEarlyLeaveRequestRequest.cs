using System;
using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class SubmitEarlyLeaveRequestRequest
{
    [Required(ErrorMessage = "Assignment ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Assignment ID must be a positive integer.")]
    public int AssignmentId { get; set; }

    [Required(ErrorMessage = "Requested time is required.")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StrictDateTimeOffsetConverter))]
    public DateTimeOffset RequestedTime { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    public string Reason { get; set; } = null!;
}
