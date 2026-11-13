namespace UI_Admin.Models.Manager;

public class ManagerContractDetailViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string? ContractTerms { get; set; }
    public int? ReplacesContractId { get; set; }

    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeePhone { get; set; } = string.Empty;
    public string EmployeeAddress { get; set; } = string.Empty;
    public string EmployeeIdentityNo { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }
    public string? CreatedByManagerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public string? ManagerRejectionComment { get; set; }

    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public DateTime? TerminationEffectiveAt { get; set; }
    public Guid? TerminatedBy { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? ContractTemplateVersion { get; set; }
    public int? OpenChangeRequestId { get; set; }
    public string? OpenChangeRequestStatus { get; set; }

    public bool HasPdf { get; set; }
    public int TerminationWarningCount { get; set; }
    public bool CanTerminateEarly { get; set; }
    public bool IsStaffCurrentlyInShift { get; set; }
    public string? StaffInShiftMessage { get; set; }
    public List<ContractTerminationWarningViewModel> TerminationWarnings { get; set; } = [];

    // History
    public ManagerContractHistoryViewModel? History { get; set; }
    public bool HistoryLoadFailed { get; set; }
    public string? HistoryErrorMessage { get; set; }
}

public class ManagerContractHistoryViewModel
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

    public List<ManagerContractHistoryEntryViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class ManagerContractHistoryEntryViewModel
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
    public List<ManagerContractChangeFieldViewModel>? ChangedFields { get; set; }
    public List<ManagerContractChangeItemViewModel>? ChangeRequestItems { get; set; }
}

public class ManagerContractChangeFieldViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class ManagerContractChangeItemViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string? Reason { get; set; }
}


public class ContractTerminationWarningViewModel
{
    public int NotificationId { get; set; }
    public int ContractId { get; set; }
    public short WarningNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid SentByManagerId { get; set; }
}
