namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public sealed class StaffShiftSwapResponse
{
    public int SwapRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = "";
    public Guid TargetUserId { get; set; }
    public string TargetUserName { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public Guid? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ShiftSwapAssignmentResponse SourceAssignment { get; set; } = new();
    public ShiftSwapAssignmentResponse TargetAssignment { get; set; } = new();
}

public sealed class ShiftSwapAssignmentResponse
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public int BranchId { get; set; }
    public string ShiftName { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
}
