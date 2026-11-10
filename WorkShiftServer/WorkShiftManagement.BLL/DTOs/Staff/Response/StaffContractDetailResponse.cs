using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public class StaffContractDetailResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string EmployeeFullName { get; set; } = null!;
    public string EmployeePosition { get; set; } = null!;
    public string? ContractTerms { get; set; }

    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string BranchAddress { get; set; } = null!;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
    public decimal? RegionalMinimumWage { get; set; }

    public string? CreatedByManagerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public bool HasPdf { get; set; }

    public bool CanAccept { get; set; }
    public string? CannotAcceptReason { get; set; }

    public bool CanSubmitChangeRequest { get; set; }
    public string? CannotSubmitChangeRequestReason { get; set; }
    public int? OpenChangeRequestId { get; set; }
    public string? OpenChangeRequestStatus { get; set; }

    public string? TerminationReason { get; set; }
    public DateTime? TerminationEffectiveAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
}
