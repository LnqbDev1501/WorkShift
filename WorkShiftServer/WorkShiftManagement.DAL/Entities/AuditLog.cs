using System;
using System.Collections.Generic;
using System.Net;

namespace WorkShiftManagement.DAL.Entities;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? TableName { get; set; }

    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public IPAddress? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
