using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.PayrollCalculation;

namespace UI_Admin.Models.PayrollDocuments;

public sealed class SubmitPayrollFeedbackRequest
{
    public string? ReasonCode { get; set; }
    [Required, StringLength(2000)]
    public string Description { get; set; } = "";
}

public sealed class ReviewPayrollFeedbackRequest
{
    [StringLength(2000)]
    public string? ResolutionNote { get; set; }
}

public sealed class CreatePayrollIssueRequest
{
    [Required, StringLength(2000)]
    public string Reason { get; set; } = "";
}

public sealed class RespondPayrollIssueRequest
{
    [Required]
    public string Action { get; set; } = ""; // CORRECT or EXPLAIN
    [Required, StringLength(2000)]
    public string Note { get; set; } = "";
}

public sealed class ReviewPayrollIssueRequest
{
    [Required]
    public string Decision { get; set; } = ""; // ACCEPT_EXPLANATION or RETURN
    [Required, StringLength(2000)]
    public string Note { get; set; } = "";
}

public sealed class CompletePayrollCorrectionRequest
{
    [Range(1, int.MaxValue)]
    public int PayrollId { get; set; }
    [Required, StringLength(2000)]
    public string Note { get; set; } = "";
}

public sealed class PayrollCorrectionRequestInput
{
    public int CorrectionRequestId { get; set; }
}

public sealed class PayrollFeedbackListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string? Status { get; set; }
    public string? ReviewStatus { get; set; }
    public string? CorrectionStatus { get; set; }
    public string? ReasonCode { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int? PayslipId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class PayrollFeedbackListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<PayrollFeedbackResponse> Items { get; set; } = [];
}

public class PayrollFeedbackResponse
{
    public int FeedbackId { get; set; }
    public int PayrollId { get; set; }
    public int PayrollVersionNo { get; set; }
    public int PayrollPeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid UserId => EmployeeId;
    public string EmployeeName { get; set; } = "";
    public int? PreviousFeedbackId { get; set; }
    public int? RoundNo { get; set; }
    public string? ReviewStatus { get; set; } // PENDING, CONFIRMED, FEEDBACK_PENDING, SUPERSEDED, EXPIRED
    public string? Status { get; set; } // SUBMITTED, APPROVED, REJECTED
    public string? Description { get; set; }
    public Guid? SentBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewDeadlineAt { get; set; }
    public DateTime? StaffRespondedAt { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public string? ResultNote { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosureReason { get; set; }
    public bool CanRespond { get; set; }
    public List<PayrollIssueResponse> Corrections { get; set; } = [];

    // Helper properties for UI compatibility
    public string? PeriodCode { get; set; }
    public string? BranchName { get; set; }
    public int? PayslipId { get; set; }
    public int? PayslipVersionNo { get; set; }
    public string? ReasonCode => Description;
    public string? CorrectionStatus => Corrections.LastOrDefault()?.Status;
    public DateTime? ResolvedAt => ProcessedAt;
    public int? CorrectionPayslipId => null;
}

public sealed class PayrollFeedbackDetailsResponse
{
    public required PayrollFeedbackResponse Feedback { get; set; }
    public required CalculatedPayroll OriginalPayroll { get; set; }
    public CalculatedPayroll? CorrectionPayroll { get; set; }
    public List<PendingPayrollAdjustment> PendingSourceAdjustments { get; set; } = [];
}

public class PayrollIssueResponse
{
    public int CorrectionRequestId { get; set; }
    public int PayrollId { get; set; }
    public int? FeedbackId { get; set; }
    public int? PreviousRequestId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = ""; // OPEN, RESPONDED, RETURNED, COMPLETED
    public Guid RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ResponseAction { get; set; } // CORRECT, EXPLAIN
    public string? ResponseNote { get; set; }
    public Guid? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public int? ResolvedPayrollId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CandidatePayrollId { get; set; }
    public bool AwaitingAccountantCompletion { get; set; }
    public int PayrollPeriodId { get; set; }
    public string? PeriodCode { get; set; }
    public int? ScheduleWeekId { get; set; }
    public string? EmployeeName { get; set; }
    public string? ManagerAction => ResponseAction;
    public string? ManagerNote => ResponseNote;
}

public class PayrollCorrectionRequestRecord : PayrollIssueResponse { }

public sealed class PayrollCorrectionPreviewResponse
{
    public int CorrectionRequestId { get; set; }
    public required CalculatedPayroll CurrentPayroll { get; set; }
    public required CalculatedPayroll Preview { get; set; }
    public bool HasChanges { get; set; }
    public bool CreatesVersion => false;
}

public sealed class PayrollSourceImpactResponse
{
    public int PayrollPeriodId { get; set; }
    public bool HasBlockingChanges { get; set; }
    public List<PayrollSourceImpactEmployee> Employees { get; set; } = [];
}

public sealed class PayrollSourceImpactEmployee
{
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int? CurrentPayrollId { get; set; }
    public int? CurrentPayrollVersionNo { get; set; }
    public string? CurrentPayrollStatus { get; set; }
    public bool HasChanges { get; set; }
    public bool HasBlockingReasons { get; set; }
    public List<string> BlockingReasons { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public int? ActiveCorrectionRequestId { get; set; }
    public string? ActiveCorrectionStatus { get; set; }
    public string? ActiveCorrectionManagerAction { get; set; }
    public int? PayrollId => CurrentPayrollId;
    public int? VersionNo => CurrentPayrollVersionNo;
    public List<string> ChangedSources => Warnings;
}

public sealed class PendingPayrollAdjustment
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
}
