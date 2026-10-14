namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public sealed class StaffShiftRegistrationResponse
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public int? ShiftRequirementId { get; set; }
    public string AssignmentSource { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime AssignedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
