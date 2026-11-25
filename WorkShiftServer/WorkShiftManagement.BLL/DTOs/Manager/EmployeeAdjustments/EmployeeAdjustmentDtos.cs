using System.ComponentModel.DataAnnotations;
namespace WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;

public sealed class EmployeeAdjustmentQuery
{
    [StringLength(200)] public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    [Range(1, int.MaxValue)] public int? PayrollPeriodId { get; set; }
    [Range(1, int.MaxValue)] public int? BranchId { get; set; }
    public string? ApprovalStatus { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    [Range(1, int.MaxValue / 100)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
public sealed class SaveEmployeeAdjustmentRequest
{
    public Guid UserId { get; set; }
    [Range(1, int.MaxValue)] public int? PayrollPeriodId { get; set; }
    public DateOnly? AdjustmentDate { get; set; }
    [Required, StringLength(100)] public string AdjustmentName { get; set; } = "";
    public decimal Amount { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
public sealed class ReviewEmployeeAdjustmentRequest
{
    [StringLength(1000)] public string? ReviewNote { get; set; }
}public sealed class EmployeeAdjustmentResponse
{
    public int RewardPenaltyId { get; set; }
    public string AdjustmentType { get; set; } = "";
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int? PayrollPeriodId { get; set; }
    public DateOnly? AdjustmentDate { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string PeriodStatus { get; set; } = "";
    public string AdjustmentName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string ApprovalStatus { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? ReviewNote { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
}
public sealed class PayrollPeriodOptionResponse
{
    public int? PayrollPeriodId { get; set; }
    public DateOnly? AdjustmentDate { get; set; }
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "";
}
