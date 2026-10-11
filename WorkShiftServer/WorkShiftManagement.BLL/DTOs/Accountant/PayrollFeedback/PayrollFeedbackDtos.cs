using System.ComponentModel.DataAnnotations;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;

namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;

public sealed class SubmitPayrollFeedbackRequest
{
    [Required, StringLength(30)]
    public string ReasonCode { get; set; } = "";
    [Required, StringLength(2000)]
    public string Description { get; set; } = "";
}

public sealed class ReviewPayrollFeedbackRequest
{
    [StringLength(2000)]
    public string? ResolutionNote { get; set; }
}

public sealed class PayrollFeedbackListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public int? PayslipId { get; set; }
    public string? Status { get; set; }
    public string? CorrectionStatus { get; set; }
    public string? ReasonCode { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
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

public sealed class PayrollFeedbackResponse
{
    public int FeedbackId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int PayrollPeriodId { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public int PayrollId { get; set; }
    public int PayrollVersionNo { get; set; }
    public int PayslipId { get; set; }
    public int PayslipVersionNo { get; set; }
    public string PayslipStatus { get; set; } = "";
    public DateTime FeedbackDeadlineAt { get; set; }
    public string ReasonCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string? CorrectionStatus { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public int? ResolvedPayrollId { get; set; }
    public int? ResolvedPayrollVersionNo { get; set; }
    public string? ResolvedPayrollStatus { get; set; }
    public int? CorrectionPayslipId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PayrollFeedbackDetailsResponse
{
    public required PayrollFeedbackResponse Feedback { get; set; }
    public required PayrollCalculationResponse OriginalPayroll { get; set; }
    public PayrollCalculationResponse? CorrectionPayroll { get; set; }
}
