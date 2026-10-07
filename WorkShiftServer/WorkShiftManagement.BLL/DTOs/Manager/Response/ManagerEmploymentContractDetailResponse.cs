using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmploymentContractDetailResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string? ContractTerms { get; set; }
    public int? ReplacesContractId { get; set; }

    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = null!;
    public string EmployeePhone { get; set; } = null!;
    public string EmployeeAddress { get; set; } = null!;
    public string EmployeeIdentityNo { get; set; } = null!;
    public string EmployeePosition { get; set; } = null!;

    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string BranchAddress { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;

    public Guid CreatedBy { get; set; }
    public string? CreatedByManagerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? TerminationEffectiveAt { get; set; }
    public Guid? TerminatedBy { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? ManagerRejectionComment { get; set; }

    public bool HasPdf { get; set; }
    public string? ContractTemplateVersion { get; set; }
    public int? OpenChangeRequestId { get; set; }
    public string? OpenChangeRequestStatus { get; set; }
}
