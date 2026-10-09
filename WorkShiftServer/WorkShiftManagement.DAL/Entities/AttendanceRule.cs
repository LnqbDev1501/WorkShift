using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class AttendanceRule
{
    public int AttendanceRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public string? Description { get; set; }

    public int CheckInBeforeMinutes { get; set; }

    public int CheckInGraceMinutes { get; set; }

    public int CheckOutGraceMinutes { get; set; }

    public int CheckOutAfterMinutes { get; set; }

    public int MaxAttendanceDistanceMeters { get; set; }

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

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? PublishedByNavigation { get; set; }
}
