namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public sealed class ShiftAssignmentResponse
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public Guid UserId { get; set; }
    public int? ShiftRequirementId { get; set; }
    public string Status { get; set; } = "";
    public string AssignmentSource { get; set; } = "";
    public Guid AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
