using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Timesheet
{
    public int TimesheetId { get; set; }

    public Guid UserId { get; set; }

    public int PayrollPeriodId { get; set; }

    public int TotalAssignedShifts { get; set; }

    public int CompletedShiftCount { get; set; }

    public int AbsentShiftCount { get; set; }

    public decimal TotalScheduledHours { get; set; }

    public decimal TotalWorkHours { get; set; }

    public int TotalLateMinutes { get; set; }

    public int ExcusedLateMinutes { get; set; }

    public int ChargeableLateMinutes { get; set; }

    public int TotalEarlyLeaveMinutes { get; set; }

    public int ExcusedEarlyLeaveMinutes { get; set; }

    public int ChargeableEarlyLeaveMinutes { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? GeneratedAt { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Payroll? Payroll { get; set; }

    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
