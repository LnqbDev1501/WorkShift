using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.SalaryChanges;

public sealed class SalaryChangeQuery
{
    public string? Keyword { get; set; }
    public Guid? UserId { get; set; }
    public int? BranchId { get; set; }
    public string? Status { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class CreateSalaryChangeRequest
{
    public Guid UserId { get; set; }
    public int? EmployeeRequestId { get; set; }
    public int? PreviousRequestId { get; set; }
    public decimal ProposedHourlyRate { get; set; }
    public DateOnly ProposedEffectiveFrom { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; } = "";
}

public sealed class ApproveSalaryChangeRequest
{
    public DateOnly? ApprovedEffectiveFrom { get; set; }
    [StringLength(1000)] public string? EffectiveDateChangeReason { get; set; }
    [StringLength(1000)] public string? ReviewNote { get; set; }
}

public sealed class RejectSalaryChangeRequest
{
    [Required, StringLength(1000)] public string ReviewNote { get; set; } = "";
}

public sealed class CancelSalaryChangeRequest
{
    [Required, StringLength(1000)] public string Reason { get; set; } = "";
}

public sealed class SalaryChangeResponse
{
    public int SalaryChangeRequestId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int? EmployeeRequestId { get; set; }
    public int? PreviousRequestId { get; set; }
    public int CurrentSalaryHistoryId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = "";
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
    public DateOnly ProposedEffectiveFrom { get; set; }
    public DateOnly? ApprovedEffectiveFrom { get; set; }
    public string? EffectiveDateChangeReason { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int? NewSalaryHistoryId { get; set; }
    public string? NewSalaryStatus { get; set; }
    public bool CanCancel { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool RequiresEffectiveDateChange { get; set; }
    public DateOnly EarliestAllowedEffectiveFrom { get; set; }
    public string? ApprovalBlockedReason { get; set; }
}
