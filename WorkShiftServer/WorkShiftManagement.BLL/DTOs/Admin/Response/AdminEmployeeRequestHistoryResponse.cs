namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class AdminEmployeeRequestHistoryResponse
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
}
