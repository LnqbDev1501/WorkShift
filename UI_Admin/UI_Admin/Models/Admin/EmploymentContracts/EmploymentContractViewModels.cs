namespace UI_Admin.Models.EmploymentContracts;

/// <summary>
/// Maps to AdminEmploymentContractListItemResponse from the API.
/// </summary>
public class EmploymentContractListItemViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasPdf { get; set; }
}

/// <summary>
/// Maps to AdminEmploymentContractDetailResponse from the API.
/// </summary>
public class EmploymentContractDetailViewModel
{
    // Contract info
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string? ContractTerms { get; set; }
    public int? ReplacesContractId { get; set; }

    // Staff info
    public Guid StaffId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdentityNo { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;

    // Organization info
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;

    // Lifecycle info
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffRespondedAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? ManagerRejectionComment { get; set; }
    public Guid? TerminatedBy { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public DateTime? TerminationEffectiveAt { get; set; }
    public string? TerminationReason { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // PDF info
    public bool HasPdf { get; set; }
    public string? ContractTemplateVersion { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }

    // History
    public EmploymentContractHistoryViewModel? History { get; set; }
    public bool HistoryLoadFailed { get; set; }
    public string? HistoryErrorMessage { get; set; }
}

public class EmploymentContractHistoryViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffFullName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public int? ReplacesContractId { get; set; }
    public string? ReplacesContractNumber { get; set; }
    public int? ReplacedByContractId { get; set; }
    public string? ReplacedByContractNumber { get; set; }

    public List<EmploymentContractHistoryEntryViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class EmploymentContractHistoryEntryViewModel
{
    public long AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionTitle { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }

    public string? TemplateVersion { get; set; }
    public string? ContractFilePath { get; set; }
    public int? ChangeRequestId { get; set; }
    public List<EmploymentContractChangeFieldViewModel>? ChangedFields { get; set; }
    public List<EmploymentContractChangeItemViewModel>? ChangeRequestItems { get; set; }
}

public class EmploymentContractChangeFieldViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class EmploymentContractChangeItemViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Wraps paged response from GET /api/admin/contracts.
/// JSON contains: Items, TotalItems, PageNumber, PageSize.
/// TotalPages is computed because it is [JsonIgnore] on the server.
/// </summary>
public class EmploymentContractPagedResponse
{
    public List<EmploymentContractListItemViewModel> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

/// <summary>
/// View model for the Index (list) page.
/// </summary>
public class EmploymentContractListViewModel
{
    public List<EmploymentContractListItemViewModel> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    // Filters
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public int? BranchIdFilter { get; set; }
    public int? RegionIdFilter { get; set; }
    public string? StartDateFrom { get; set; }
    public string? StartDateTo { get; set; }
}
