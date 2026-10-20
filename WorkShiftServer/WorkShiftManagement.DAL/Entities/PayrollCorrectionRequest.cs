using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollCorrectionRequest
{
    public int CorrectionRequestId { get; set; }

    public int PayrollId { get; set; }

    public int? FeedbackId { get; set; }

    public int? PreviousRequestId { get; set; }

    public string Reason { get; set; } = null!;

    public Guid RequestedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? ResponseAction { get; set; }

    public string? ResponseNote { get; set; }

    public Guid? RespondedBy { get; set; }

    public DateTime? RespondedAt { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public int? ResolvedPayrollId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime ResponseDeadlineAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual PayrollFeedbackRequest? Feedback { get; set; }

    public virtual PayrollCorrectionRequest? InversePreviousRequest { get; set; }

    public virtual Payroll Payroll { get; set; } = null!;

    public virtual PayrollCorrectionRequest? PreviousRequest { get; set; }

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual Payroll? ResolvedPayroll { get; set; }

    public virtual User? RespondedByNavigation { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }
}
