namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerContractHistoryResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string CurrentStatus { get; set; } = null!;
    public Guid StaffUserId { get; set; }
    public string StaffFullName { get; set; } = null!;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;

    public int? ReplacesContractId { get; set; }
    public string? ReplacesContractNumber { get; set; }
    public int? ReplacedByContractId { get; set; }
    public string? ReplacedByContractNumber { get; set; }

    public IReadOnlyList<ContractHistoryEntryDto> Items { get; set; } = new List<ContractHistoryEntryDto>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 20));
}

public class ContractHistoryEntryDto
{
    public long AuditLogId { get; set; }
    public string Action { get; set; } = null!;
    public string ActionTitle { get; set; } = null!;
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = null!;
    public string ActorRole { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }

    public string? TemplateVersion { get; set; }
    public string? ContractFilePath { get; set; }
    public int? ChangeRequestId { get; set; }
    public List<ContractHistoryChangeFieldDto>? ChangedFields { get; set; }
    public List<ContractHistoryChangeItemDto>? ChangeRequestItems { get; set; }
}

public class ContractHistoryChangeFieldDto
{
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class ContractHistoryChangeItemDto
{
    public string FieldName { get; set; } = null!;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = null!;
    public string? Reason { get; set; }
}
