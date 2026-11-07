using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Accountant.PayrollSettlement;

public sealed class ConfirmPayrollSettlementRequest
{
    public bool ConfirmedExternalSettlement { get; set; }
    [StringLength(100)] public string? PaymentReference { get; set; }
    [StringLength(2000)] public string? PaymentNote { get; set; }
}

public sealed class PayrollSettlementEmployeeResponse
{
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int PayrollId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = "";
    public int? PayslipId { get; set; }
    public decimal NetSalary { get; set; }
    public decimal CarryForwardDeduction { get; set; }
    public decimal DeferredDeductionAmount { get; set; }
}

public sealed class PayrollSettlementSummaryResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodCode { get; set; } = "";
    public int BranchId { get; set; }
    public string PeriodStatus { get; set; } = "";
    public DateTime FeedbackDeadlineAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsSettled { get; set; }
    public bool CanConfirmSettlement { get; set; }
    public int ExpectedEmployeeCount { get; set; }
    public int UnresolvedFeedbackCount { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalCarryForwardApplied { get; set; }
    public decimal TotalNewCarryForward { get; set; }
    public List<string> BlockingReasons { get; set; } = [];
    public List<PayrollSettlementEmployeeResponse> Employees { get; set; } = [];
}

public sealed class PayrollSettlementConfirmationResponse
{
    public bool AlreadySettled { get; set; }
    public required PayrollSettlementSummaryResponse Summary { get; set; }
}

public sealed class CarryForwardListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public int? SourcePayrollId { get; set; }
    public int? SourcePayrollPeriodId { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class CarryForwardResponse
{
    public int CarryForwardId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string? EmployeeType { get; set; }
    public string EmployeeStatus { get; set; } = "ACTIVE";
    public int SourceBranchId { get; set; }
    public int SourcePayrollId { get; set; }
    public int SourcePayrollVersionNo { get; set; }
    public int SourcePayrollPeriodId { get; set; }
    public string SourcePeriodCode { get; set; } = "";
    public string SourcePeriodName { get; set; } = "";
    public int SourcePayrollDetailId { get; set; }
    public string SourceDetailType { get; set; } = "";
    public string SourceDetailDescription { get; set; } = "";
    public string? SourceDetailNote { get; set; }
    public int? PenaltyId { get; set; }
    public decimal SourceDeductionAmount { get; set; }
    public decimal SourceAppliedAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal TotalAppliedAmount { get; set; }
    public int ApplicationCount { get; set; }
    public DateTime? LastAppliedAt { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CarryForwardListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<CarryForwardResponse> Items { get; set; } = [];
}

public sealed class CarryForwardApplicationResponse
{
    public int ApplicationId { get; set; }
    public int CarryForwardId { get; set; }
    public Guid UserId { get; set; }
    public int PayrollId { get; set; }
    public int PayrollVersionNo { get; set; }
    public string PayrollStatus { get; set; } = "";
    public int PayrollPeriodId { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal RemainingBalanceAfter { get; set; }
    public DateTime AppliedAt { get; set; }
}

public sealed class CarryForwardApplicationListResponse
{
    public int CarryForwardId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<CarryForwardApplicationResponse> Items { get; set; } = [];
}

public sealed class CarryForwardListViewModel
{
    public CarryForwardListRequest Query { get; set; } = new();
    public CarryForwardListResponse Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class CarryForwardDetailViewModel
{
    public CarryForwardResponse Item { get; set; } = new();
    public CarryForwardApplicationListResponse Applications { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
