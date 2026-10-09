using System.Text.Json;
namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;

public sealed class PayrollCalculationListRequest
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
public sealed class EmployeeCalculationReadiness
{
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int? TimesheetId { get; set; }
    public int? ExistingPayrollId { get; set; }
    public string? ExistingPayrollStatus { get; set; }
    public bool CanCalculate { get; set; }
    public List<string> BlockingReasons { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
public sealed class CalculationReadinessResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodStatus { get; set; } = "";
    public List<EmployeeCalculationReadiness> Employees { get; set; } = [];
}
public sealed class CalculationBatchResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodStatus { get; set; } = "";
    public List<PayrollCalculationResponse> Payrolls { get; set; } = [];
    public List<EmployeeCalculationReadiness> FailedEmployees { get; set; } = [];
}
public sealed class PayrollCalculationDetailResponse
{
    public int PayrollDetailId { get; set; }
    public string Type { get; set; } = "";
    public string? ItemCode { get; set; }
    public string Description { get; set; } = "";
    public DateOnly? WorkFrom { get; set; }
    public DateOnly? WorkTo { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Rate { get; set; }
    public decimal Multiplier { get; set; }
    public decimal Amount { get; set; }
    public decimal? AppliedAmount { get; set; }
    public int? SalaryHistoryId { get; set; }
    public int? PublicHolidayId { get; set; }
    public int? HolidayPayRuleId { get; set; }
    public int? RewardPenaltyId { get; set; }
    public int? AttendancePenaltyId { get; set; }
    public int? CarryForwardId { get; set; }
    public int DisplayOrder { get; set; }
    public string? Note { get; set; }
}
public sealed class PayrollCalculationResponse
{
    public int PayrollId { get; set; }
    public Guid UserId { get; set; }
    public int PayrollPeriodId { get; set; }
    public int BranchId { get; set; }
    public int TimesheetId { get; set; }
    public int SalaryHistoryId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string? EmployeePosition { get; set; }
    public string BranchName { get; set; } = "";
    public string Status { get; set; } = "";
    public int VersionNo { get; set; }
    public int? PreviousPayrollId { get; set; }
    public string? CorrectionReason { get; set; }
    public string? RejectionCategory { get; set; }
    public string? RejectReason { get; set; }
    public Guid? CalculatedBy { get; set; }
    public DateTime? CalculatedAt { get; set; }
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
    public JsonElement? WorkInputSnapshot { get; set; }
    public List<PayrollCalculationDetailResponse> Details { get; set; } = [];
}
