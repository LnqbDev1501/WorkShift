using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmployeeRequestHistoryResponse
{
    public long AuditLogId { get; set; }

    public string Action { get; set; } = null!;

    public Guid? UserId { get; set; }

    public string? FullName { get; set; }

    public string? RoleName { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Note { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
