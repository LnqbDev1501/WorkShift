using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class PayrollFeedbackRequest
{
    public int FeedbackId { get; set; }

    public int PayrollId { get; set; }

    public Guid EmployeeId { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public Guid? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? PreviousFeedbackId { get; set; }

    public int? RoundNo { get; set; }

    public Guid? SentBy { get; set; }

    public DateTime? ReviewDeadlineAt { get; set; }

    public string? ResultNote { get; set; }

    public string? ReviewStatus { get; set; }

    public DateTime? StaffRespondedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? ClosureReason { get; set; }

    public virtual User Employee { get; set; } = null!;

    public virtual ICollection<PayrollFeedbackRequest> InversePreviousFeedback { get; set; } = new List<PayrollFeedbackRequest>();

    public virtual Payroll Payroll { get; set; } = null!;

    public virtual ICollection<PayrollCorrectionRequest> PayrollCorrectionRequests { get; set; } = new List<PayrollCorrectionRequest>();

    public virtual PayrollFeedbackRequest? PreviousFeedback { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User? SentByNavigation { get; set; }
}
