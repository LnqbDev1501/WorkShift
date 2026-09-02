using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollPeriod
{
    public int PayrollPeriodId { get; set; }

    public int BranchId { get; set; }

    public string BranchNameSnapshot { get; set; } = null!;

    public string PeriodCode { get; set; } = null!;

    public string PeriodName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public Guid? CalculatedBy { get; set; }

    public DateTime? CalculatedAt { get; set; }

    public Guid? SubmittedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User? CalculatedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenalties { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual User? RejectedByNavigation { get; set; }

    public virtual User? SubmittedByNavigation { get; set; }

    public virtual ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
