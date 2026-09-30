namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public sealed class ShiftCancellationRequestResponse
{
    public int RequestId { get; set; }
    public int BranchId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Reason { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public List<ShiftCancellationAssignmentResponse> Assignments { get; set; } = [];
}

public sealed class ShiftCancellationAssignmentResponse
{
    public int AssignmentId { get; set; }
    public string AssignmentStatus { get; set; } = "";
    public int ShiftId { get; set; }
    public int ScheduleWeekId { get; set; }
    public string ShiftName { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string ShiftStatus { get; set; } = "";
    public int? EmployeeTypeId { get; set; }
    public string? EmployeeTypeName { get; set; }
}
