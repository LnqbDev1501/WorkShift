using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollFeedbackRequest
{
    public int FeedbackId { get; set; }

    public int PayrollId { get; set; }

    public int PayslipId { get; set; }

    public Guid EmployeeId { get; set; }

    public string ReasonCode { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public string? CorrectionStatus { get; set; }

    public int? ResolvedPayrollId { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Employee { get; set; } = null!;

    public virtual Payroll Payroll { get; set; } = null!;

    public virtual Payslip Payslip { get; set; } = null!;

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual Payroll? ResolvedPayroll { get; set; }
}
