using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class WorkScheduleWeek
{
    public int ScheduleWeekId { get; set; }

    public int BranchId { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public DateTime RegistrationDeadline { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? RegistrationClosedAt { get; set; }

    public Guid? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? AbsenceLimitRuleId { get; set; }

    public virtual AbsenceLimitRule? AbsenceLimitRule { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PayrollPeriod? PayrollPeriod { get; set; }

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}
