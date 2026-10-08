using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Staff.Contract;

public class StaffContractListViewModel
{
    public IReadOnlyList<StaffContractListItemViewModel> Items { get; set; } = [];
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public string? StartDateFrom { get; set; }
    public string? StartDateTo { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StaffContractListItemViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string EmployeePosition { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public bool HasPdf { get; set; }
}

public class StaffContractDetailViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string EmployeePosition { get; set; } = string.Empty;
    public string? ContractTerms { get; set; }

    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;

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

    public IReadOnlyList<StaffContractChangeRequestViewModel> ChangeRequests { get; set; } = [];
    public SubmitStaffContractChangeRequestViewModel ChangeRequestForm { get; set; } = new();
}

public class StaffContractChangeRequestViewModel
{
    public int ChangeRequestId { get; set; }
    public int ContractId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? StaffComment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<StaffContractChangeRequestItemViewModel> Items { get; set; } = [];
}

public class StaffContractChangeRequestItemViewModel
{
    public int ChangeRequestItemId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class SubmitStaffContractChangeRequestViewModel
{
    public int ContractId { get; set; }

    public string RequestType { get; set; } = "CHANGE_REQUEST";

    [MaxLength(1000, ErrorMessage = "Ghi chú của nhân viên tối đa 1000 ký tự.")]
    public string? StaffComment { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trường thông tin cần thay đổi.")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá trị đề xuất mới.")]
    [MaxLength(500, ErrorMessage = "Giá trị đề xuất tối đa 500 ký tự.")]
    public string ProposedValue { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Lý do đề xuất tối đa 500 ký tự.")]
    public string? Reason { get; set; }
}
