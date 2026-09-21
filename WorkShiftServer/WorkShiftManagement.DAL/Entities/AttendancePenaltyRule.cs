using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class AttendancePenaltyRule
{
    public int PenaltyRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public Guid? PublishedBy { get; set; }

    public DateTime? PublishedAt { get; set; }

    public Guid? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AttendancePenaltyLevel> AttendancePenaltyLevels { get; set; } = new List<AttendancePenaltyLevel>();

    public virtual ICollection<AttendancePenaltyRecord> AttendancePenaltyRecords { get; set; } = new List<AttendancePenaltyRecord>();

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? PublishedByNavigation { get; set; }
}
