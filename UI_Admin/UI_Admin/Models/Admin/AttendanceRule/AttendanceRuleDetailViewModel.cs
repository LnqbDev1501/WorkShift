namespace UI_Admin.Models.Admin.AttendanceRule;

public sealed class AttendanceRuleDetailViewModel
{
    public int AttendanceRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CheckInBeforeMinutes { get; set; }
    public int CheckInGraceMinutes { get; set; }
    public int CheckOutGraceMinutes { get; set; }
    public int CheckOutAfterMinutes { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // State Helpers
    public bool IsDraft => Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase);
    public bool IsScheduled => Status.Equals("SCHEDULED", StringComparison.OrdinalIgnoreCase);
    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsCancelled => Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);

    // Permission / Capability Helpers
    public bool CanPublish => IsDraft;
    public bool CanCancel => IsScheduled || IsActive;
    public bool CanDelete => IsDraft;
    public bool CanEdit => IsDraft;
}
