using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;

namespace WorkShiftManagement.BLL.DTOs.Accountant.Payslip;

public sealed class PayslipListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public int? BranchId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string? Status { get; set; }
    // Include replaced versions only when explicitly requested; history is also available separately.
    public bool IncludeHistory { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class PayslipListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<PayslipSummaryResponse> Items { get; set; } = [];
}

public class PayslipSummaryResponse
{
    public int PayslipId { get; set; }
    public int PayrollId { get; set; }
    public int PayrollPeriodId { get; set; }
    public Guid UserId { get; set; }
    public int BranchId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string? EmployeePosition { get; set; }
    public string BranchName { get; set; } = "";
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int VersionNo { get; set; }
    public int? PreviousPayslipId { get; set; }
    public string Status { get; set; } = "";
    public string PayrollStatus { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public DateTime? ReplacedAt { get; set; }
    public DateTime FeedbackDeadlineAt { get; set; }
    public bool CanSubmitFeedback { get; set; }
    public string? FeedbackBlockedReason { get; set; }
    public string? CorrectionReason { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalWorkHours { get; set; }
    public decimal WorkPay { get; set; }
    public decimal TotalReward { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal AttendanceDeduction { get; set; }
    public decimal CarryForwardDeduction { get; set; }
    public decimal DeferredDeductionAmount { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalary { get; set; }
}

public sealed class PayslipDetailsResponse
{
    public required PayslipSummaryResponse Payslip { get; set; }
    public List<PayslipWorkDetailsResponse> WorkDetails { get; set; } = [];
    public List<PayrollCalculationDetailResponse> CalculationDetails { get; set; } = [];
}

public sealed class PayslipWorkDetailsResponse
{
    public int PayslipWorkDetailId { get; set; }
    public int SourceAssignmentId { get; set; }
    public int? SourceAttendanceId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string ShiftName { get; set; } = "";
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public int BreakMinutes { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string AttendanceStatus { get; set; } = "";
    public int ActualWorkingMinutes { get; set; }
    public int PayableMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int ExcusedLateMinutes { get; set; }
    public int ChargeableLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public int ExcusedEarlyLeaveMinutes { get; set; }
    public int ChargeableEarlyLeaveMinutes { get; set; }
    public string? Note { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class PayslipVersionHistoryResponse
{
    public Guid UserId { get; set; }
    public int PayrollPeriodId { get; set; }
    public int? CurrentPayslipId { get; set; }
    public List<PayslipSummaryResponse> Versions { get; set; } = [];
}
