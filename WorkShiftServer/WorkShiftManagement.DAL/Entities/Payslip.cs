using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Payslip
{
    public int PayslipId { get; set; }

    public int PayrollId { get; set; }

    public int VersionNo { get; set; }

    public int? PreviousPayslipId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime PublishedAt { get; set; }

    public DateTime? ReplacedAt { get; set; }

    public string PeriodCodeSnapshot { get; set; } = null!;

    public string PeriodNameSnapshot { get; set; } = null!;

    public DateOnly PeriodStartSnapshot { get; set; }

    public DateOnly PeriodEndSnapshot { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payslip? InversePreviousPayslip { get; set; }

    public virtual Payroll Payroll { get; set; } = null!;

    public virtual ICollection<PayrollFeedbackRequest> PayrollFeedbackRequests { get; set; } = new List<PayrollFeedbackRequest>();

    public virtual ICollection<PayslipWorkDetail> PayslipWorkDetails { get; set; } = new List<PayslipWorkDetail>();

    public virtual Payslip? PreviousPayslip { get; set; }
}
