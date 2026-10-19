using System.ComponentModel.DataAnnotations;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
public sealed class SubmitPayrollFeedbackRequest
{ [Required, StringLength(2000)] public string Description { get; set; } = ""; }
public sealed class ReviewPayrollFeedbackRequest
{ [StringLength(2000)] public string? ResolutionNote { get; set; } }
public sealed class CreatePayrollIssueRequest
{ [Required, StringLength(2000)] public string Reason { get; set; } = ""; }
public sealed class RespondPayrollIssueRequest
{
    [Required] public string Action { get; set; } = "";
    [Required, StringLength(2000)] public string Note { get; set; } = "";
}
public sealed class ReviewPayrollIssueRequest
{
    [Required] public string Decision { get; set; } = "";
    [Required, StringLength(2000)] public string Note { get; set; } = "";
}
public sealed class PayrollFeedbackListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string? Status { get; set; }
    public string? ReviewStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
public sealed class PayrollFeedbackResponse
{
    public int FeedbackId { get; set; }
    public int PayrollId { get; set; }
    public int PayrollVersionNo { get; set; }
    public int PayrollPeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int? PreviousFeedbackId { get; set; }
    public int? RoundNo { get; set; }
    public string? ReviewStatus { get; set; }
    public string? Status { get; set; }
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
}
public sealed class PayrollFeedbackDetailsResponse
{
    public required PayrollFeedbackResponse Feedback { get; set; }
    public required PayrollCalculationResponse OriginalPayroll { get; set; }
    public List<PendingPayrollAdjustment> PendingSourceAdjustments { get; set; } = [];
}
public sealed class PayrollIssueResponse
{
    public int CorrectionRequestId { get; set; }
    public int PayrollId { get; set; }
    public int? FeedbackId { get; set; }
    public int? PreviousRequestId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ResponseAction { get; set; }
    public string? ResponseNote { get; set; }
    public Guid? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public int? ResolvedPayrollId { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class PendingPayrollAdjustment
{
    public int RewardPenaltyId { get; set; }
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status => "PENDING";
    public bool IncludedInPayroll => false;
}
