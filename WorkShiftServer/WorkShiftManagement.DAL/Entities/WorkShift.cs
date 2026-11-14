using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class WorkShift
{
    public int ShiftId { get; set; }

    public int ScheduleWeekId { get; set; }

    public int ShiftTemplateId { get; set; }

    public string ShiftNameSnapshot { get; set; } = null!;

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }

    public int RequiredEmployees { get; set; }

    public string? Note { get; set; }

    public Guid CreatedBy { get; set; }

    public string Status { get; set; } = null!;

    public Guid? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual WorkScheduleWeek ScheduleWeek { get; set; } = null!;

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftEmployeeRequirement> ShiftEmployeeRequirements { get; set; } = new List<ShiftEmployeeRequirement>();

    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;
}
