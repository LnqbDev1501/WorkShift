namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmployeeRequestHistoryResponse
{
    public long AuditLogId { get; set; }

    public int RequestId { get; set; }

    public string Action { get; set; } = null!;

    public Guid? PerformedByUserId { get; set; }

    public string? PerformedByName { get; set; }

    public string? RoleName { get; set; }

    public DateTime PerformedAt { get; set; }

    public string? PreviousStatus { get; set; }

    public string? NewStatus { get; set; }

    public string? ReviewNote { get; set; }

    // Compatibility properties
    public Guid? UserId => PerformedByUserId;
    public string? FullName => PerformedByName;
    public DateTime Timestamp => PerformedAt;
    public string? Note => ReviewNote;
}
